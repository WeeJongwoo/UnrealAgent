using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Auth;
using UnrealAgent.Backend.Mcp;
using UnrealAgent.Backend.Model;
using UnrealAgent.Backend.Model.Models;
using UnrealAgent.Backend.Prompt;
using UnrealAgent.Backend.Tool;
using UnrealAgent.Backend.Tool.Tools;
using UnrealAgent.Frontend.Infrastructure;

// ── WebApplicationBuilder (서비스 등록 + 앱 설정을 담는 빌더) 생성 ──
WebApplicationBuilder Builder = WebApplication.CreateBuilder(args);

// ── Kestrel (요청을 받아서 넘겨주는 서버 엔진) 포트 설정 ──
Builder.WebHost.UseUrls("http://localhost:55558");

// ── 정적 웹 자산 강제 로드 ──
Builder.WebHost.UseStaticWebAssets();

// ── Blazor 서비스 등록 (Razor 컴포넌트 + 서버 측 인터랙티브 모드) ──
Builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ── HTTP 클라이언트 등록 (외부 API 호출용) ──
Builder.Services.AddHttpClient("OAuth", C => C.Timeout = TimeSpan.FromSeconds(30));
Builder.Services.AddHttpClient("WebFetch");

// ── Auth 모듈 ──
Builder.Services.AddSingleton<AuthConfig>();

// ── Agent 모듈 (에이전트 루프 + 세션) ──
Builder.Services.AddSingleton<AgentSession>();
Builder.Services.AddSingleton<AgentLoop>();

// ── AgentRunner (메시지 큐 + 에이전트 루프 서비스) ──
Builder.Services.AddSingleton<AgentRunner>();
Builder.Services.AddHostedService(Sp => Sp.GetRequiredService<AgentRunner>());

// ── Runtime 모듈 ──
Builder.Services.AddSingleton<PromptBuilder>();

// ── Tool 모듈 ──
Builder.Services.AddSingleton<ToolRegistry>();
Builder.Services.AddSingleton<ToolExecutor>();


// ── Claude 모델 레지스트리 & 런타임 설정 ──
Builder.Services.AddSingleton<ModelRegistry>();
Builder.Services.AddSingleton<ModelSettings>();



// 여기까지 서비스 등록 단계. Build() 이후는 미들웨어/라우팅 설정 단계입니다.
WebApplication App = Builder.Build();

// ── 어트리뷰트 기반 자동 스캔 ──
App.Services.GetRequiredService<ToolRegistry>().DiscoverTools(typeof(WebSearch).Assembly);
App.Services.GetRequiredService<ModelRegistry>().DiscoverModels(typeof(Opus46).Assembly);

// ── 설정 로드 ──
App.Services.GetRequiredService<AuthConfig>().Load();
App.Services.GetRequiredService<ModelSettings>().Load();

// ── Auth 설정 로드 ──
App.Services.GetRequiredService<AuthConfig>().Load();

// ── 어트리뷰트 기반 자동 스캔 ──
App.Services.GetRequiredService<ToolRegistry>().DiscoverTools(typeof(WebSearch).Assembly);

// ── MCP 서버 연결 + 도구 등록 ──
{
    ToolRegistry Registry = App.Services.GetRequiredService<ToolRegistry>();
    IHttpClientFactory HttpFactory = App.Services.GetRequiredService<IHttpClientFactory>();

    foreach ((string Name, McpConfig.McpServerConfig Config) in McpConfig.Load())
    {
        HttpClient Http = HttpFactory.CreateClient();
        McpClient Client = new(Http, Name, Config.Url);

        try
        {
            await Client.InitializeAsync();

            if (Client.HasTools)
            {
                List<McpToolDefinition> Tools = await Client.ListToolsAsync();
                Registry.RegisterMcpTools(Name, Client, Tools);
            }
        }
        catch (Exception Ex)
        {
            Console.WriteLine($"[MCP] {Name} 연결 실패: {Ex.Message}");
        }
    }
}

// ── 미들웨어 파이프라인 ──
App.UseStaticFiles();
App.UseAntiforgery();

// ── Blazor 엔드포인트 (Razor 컴포넌트 라우팅 + 서버 렌더 모드 적용) ──
App.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// ── 서버 실행 (요청 수신 대기 시작) - http://localhost:55558/ ──
App.Run();