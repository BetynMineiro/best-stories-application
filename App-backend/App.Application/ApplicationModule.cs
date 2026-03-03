using App.Application.GetBestStories;
using App.CrossCutting.Notification;
using App.CrossCutting.RequestObjects;
using App.CrossCutting.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application;

public static class ApplicationModule
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IGetBestStoriesUseCase, GetBestStoriesUseCase>();
        services.AddScoped<IValidator<PagedRequest>, PagedRequestValidator>();
    }
}
