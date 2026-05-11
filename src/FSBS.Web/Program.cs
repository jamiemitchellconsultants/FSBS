using Blazored.LocalStorage;
using Fluxor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using FSBS.Web;
using FSBS.Web.Auth;
using FSBS.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddTransient<ApiResilienceHandler>();
builder.Services.AddScoped(_ =>
{
    var handler = new ApiResilienceHandler { InnerHandler = new HttpClientHandler() };
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

builder.Services.AddMudServices();
builder.Services.AddFluxor(options => options.ScanAssemblies(typeof(Program).Assembly));
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CognitoAuthStateProvider>();
builder.Services.AddScoped<CognitoAuthStateProvider>(sp =>
    (CognitoAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<OrganisationService>();
builder.Services.AddScoped<SimulatorService>();
builder.Services.AddScoped<AircraftTypeService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<ReferenceDataService>();
builder.Services.AddScoped<InstructorScheduleService>();
builder.Services.AddScoped<AvailabilityHubClient>();

await builder.Build().RunAsync();
