using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MathMaturaProject.API.DTOs;
using MathMaturaProject.API.DTOs.Feedback;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MathMaturaProject.Tests;

public class FeedbackEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> Send(object body, string? token = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/feedback") { Content = JsonContent.Create(body) };
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(req);
    }

    private async Task<FeedbackDto> SendOk(object body, string? token = null)
    {
        var res = await Send(body, token);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<FeedbackDto>())!;
    }

    private AppDbContext Db() => factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Guest_feedback_is_saved_trimmed_and_without_a_user()
    {
        var body = await SendOk(new { type = " suggestion ", title = "  Добавете тъмна тема  ", content = "  Ще е по-лесно вечер.  " });

        Assert.Equal("Suggestion", body.Type);
        Assert.Equal("Добавете тъмна тема", body.Title);

        var saved = await Db().Feedbacks.SingleAsync(f => f.Id == body.Id);
        Assert.Equal(FeedbackType.Suggestion, saved.Type);
        Assert.Equal("Добавете тъмна тема", saved.Title);
        Assert.Equal("Ще е по-лесно вечер.", saved.Content);
        Assert.Null(saved.UserId);
    }

    [Fact]
    public async Task Feedback_from_a_logged_in_user_is_linked_to_them()
    {
        var reg = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = $"student-{Guid.NewGuid():N}@example.com", displayName = "Мария", password = "parola123" });
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!;

        var body = await SendOk(new { type = "Problem", title = "Грешка в урок 3", content = "Отговорът на задача 5 е грешен." }, auth.AccessToken);

        var saved = await Db().Feedbacks.SingleAsync(f => f.Id == body.Id);
        Assert.Equal(auth.User.UserId, saved.UserId);
        Assert.Equal(FeedbackType.Problem, saved.Type);
    }

    [Fact]
    public async Task Invalid_feedback_is_refused_with_codes_and_not_saved()
    {
        var title = $"bad-{Guid.NewGuid():N}"[..9];
        var res = await Send(new { type = "Question", title, content = new string('a', 1001) });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var error = (await res.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal("VALIDATION_FAILED", error.Code);
        Assert.Equal(["TYPE_INVALID"], error.Errors!["type"]);
        Assert.Equal(["TITLE_TOO_SHORT"], error.Errors["title"]);
        Assert.Equal(["CONTENT_TOO_LONG"], error.Errors["content"]);
        Assert.False(await Db().Feedbacks.AnyAsync(f => f.Title == title));
    }

    [Fact]
    public async Task Missing_fields_are_refused()
    {
        var res = await Send(new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var error = (await res.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal(["content", "title", "type"], error.Errors!.Keys.Order());
    }

    [Fact]
    public async Task A_number_is_not_accepted_as_the_type()
        => Assert.Equal(HttpStatusCode.BadRequest, (await Send(new { type = "0", title = "Грешка в урок 3", content = "Отговорът е грешен." })).StatusCode);
}
