using SharedNotes.Api.Identity;
using SharedNotes.Api.Repositories;
using SharedNotes.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the in-memory repository and note service with the ASP.NET Core dependency injection container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();
builder.Services.AddSingleton<INoteRepository, ConcurrentDictionaryNoteRepository>();
builder.Services.AddSingleton<ITeamRepository, ConcurrentDictionaryTeamRepository>();
builder.Services.AddScoped<IUnitOfWork, ConcurrentDictionaryUnitOfWork>();
builder.Services.AddScoped<INoteService, NoteService>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
