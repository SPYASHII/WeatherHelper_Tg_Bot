using Microsoft.VisualBasic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using WeatherAPI.Models;
using WeatherHelperTGBOT.Constant;
using WeatherHelperTGBOT.Services;
using Constants = WeatherHelperTGBOT.Constant.Constants;

class Program
{
    static ITelegramBotClient bot = new TelegramBotClient("6198228281:AAEwaFhAqYRT2EtQYtQMXe5acd8bDVjHHAE");
    static BotApiService botService = new BotApiService();
    static HttpClient httpClient = new HttpClient(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    });
    static void Main(string[] args)
    {
        bot.StartReceiving(
            Update,
            Error
        );

        var Notification = new NotificationService(bot);

        Notification.Start();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }

    public static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            var message = update.Message;

            if (message == null) return;

            var id = message.Chat.Id;

            if (message.Text != null)
            {
                Console.WriteLine($"{message.Chat.Username ?? $"{message.Chat.FirstName}"}  |   {message.Text}");
            }

            UserData user = await botService.GetUserData(message);
            if (user == null)
            {
                await botService.CreateUser(message);
                user = await botService.GetUserData(message);
            }

            if (user.waitingForTime)
            {
                if (message.Text != null)
                {
                    string pattern = @"^\d{2}:\d{2}$";
                    bool isValid = Regex.IsMatch(message.Text, pattern);

                    if (!isValid)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не коректні дані! Треба ввести дані у вигляді hh:mm!");
                        return;
                    }
                    user.notification.notificationTime = DateTime.Parse(message.Text);
                    await botService.PatchNotification(user.notification, message);

                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitT=false", null);

                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Час успішно доданий!");
                        return;
                    }
                    throw new Exception(response.StatusCode.ToString());

                }
            }

            if (user.waitingForDays)
            {
                if (message.Text != null)
                {
                    int days;
                    try
                    {
                        days = int.Parse(message.Text);

                        if (days < 1 || days > 14)
                        {
                            throw new Exception("day");
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не коректні дані! Тяжко ввести цифру від 1 до 14?");
                        return;
                    }
                    user.notification.days = days;
                    await botService.PatchNotification(user.notification, message);

                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitD=false", null);

                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Кількість днів успішно додана!");
                        return;
                    }
                    throw new Exception(response.StatusCode.ToString());

                }
            }

            if (user.waitingForWeather)
            {
                if (message.Text != null)
                {
                    user.notification.weather = message.Text;

                    await botService.PatchNotification(user.notification, message);

                    var response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitW=false", null);

                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Погода успішно додана!");
                        return;
                    }

                    throw new Exception(response.StatusCode.ToString());
                }
            }

            if (user.dayByWeather)
            {
                if (message.Text != null)
                {
                    if (await botService.GetDayByWeather(botClient, message.Chat.Id, user.location, message.Text) == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Нічого не знайдено");
                    }

                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?dw=false", null);
                    if (response.IsSuccessStatusCode)
                        return;
                    throw new Exception(response.StatusCode.ToString());
                }
            }

            if (user.foreCast)
            {
                if (message.Text != null)
                {
                    int days;
                    try
                    {
                        days = int.Parse(message.Text);

                        if (days < 1 || days > 14)
                        {
                            throw new Exception("day");
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не коректні дані! Тяжко ввести цифру від 1 до 14?");
                        return;
                    }
                    await botService.GetForecast(botClient, message, user.location, days);

                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?f=false", null);
                    if (response.IsSuccessStatusCode)
                        return;
                    throw new Exception(response.StatusCode.ToString());
                }
            }

            if (user.weatherByDate)
            {
                if (message.Text != null)
                {
                    string pattern = @"^\d{4}-\d{2}-\d{2}$";
                    bool isValid = Regex.IsMatch(message.Text, pattern);

                    if (!isValid)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не коректні дані! Треба ввести дані у вигляді yyyy-mm-dd!");
                        return;
                    }

                    if (await botService.GetWeatherByDate(botClient, message, user.location, message.Text, user.gptByDate) == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Щось пішло не так.\nМожливо ви ввели дату за діапазоном у 1 рік від сьогодні.\nСпробуйте ще раз.");
                        return;
                    }

                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?wd=false", null);
                    if (response.IsSuccessStatusCode)
                        return;
                    throw new Exception(response.StatusCode.ToString());
                }
            }

            if (user.waitingForLocation)
            {
                bool c = false;

                if (message.Location != null) c = true;

                if (!c)
                {
                    if (message.Text != null)
                    {
                        HttpResponseMessage check = await httpClient.GetAsync(Constants.url + $"/location/find/{message.Text}");
                        if (!check.IsSuccessStatusCode)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Нажаль не вдалося знайти такий населений пункт :(");
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Спробуйте написати назву англійською чи надіслати геолокацію.");
                            return;
                        }
                    }
                    else return;
                }

                await botService.AddLocation(message, c);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Локація успішно додана!");

                HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitL=false", null);
                if (response.IsSuccessStatusCode)
                    return;
                throw new Exception(response.StatusCode.ToString());
            }

            if (message.Text != null)
            {

                if (message.Text == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вітаю! Я буду допомагати вам з погодою.");
                    return;
                }

                if (message.Text == "/location")
                {
                    if (user.location == null)
                    {
                        await botClient.SendTextMessageAsync(id, "В мене немає вашої локації :(");
                        return;
                    }
                    await botClient.SendTextMessageAsync(id, $"Ваша локація:{user.location}");
                    return;
                }

                if (message.Text == "/set_location" || user.location == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, надішліть назву населеного пункту або геолокацію.");
                    HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitL=true", null);
                    if (response.IsSuccessStatusCode)
                        return;
                    throw new Exception(response.StatusCode.ToString());
                }

                if (user.location != null)
                {
                    if (message.Text == "/current")
                    {
                        await botService.GetCurrentWeather(botClient, message, user.location, user.gptCurrent);
                        return;
                    }
                    if (message.Text == "/forecast")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, надішліть кількість днів прогнозу(Максимальна 14).");

                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?f=true", null);
                        if (response.IsSuccessStatusCode)
                            return;
                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/weather_by_date")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, надішліть дату на яку б ви хотіли отримати прогноз у форматі yyyy-mm-dd.");

                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?wd=true", null);
                        if (response.IsSuccessStatusCode)
                            return;
                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/day_by_weather")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, введіть погоду яку ви хочете знайти.Наприклад : Дощ, Сонячно");

                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?dw=true", null);
                        if (response.IsSuccessStatusCode)
                            return;
                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/gpt_current")
                    {
                        if (user.gptCurrent)
                        {
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?gptC=false", null);
                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "ChatGPT для поточної погоди ВИМКНЕНО.");
                                return;
                            }
                            throw new Exception(response.StatusCode.ToString());

                        }
                        else
                        {
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?gptC=true", null);
                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "ChatGPT для поточної погоди УВІМКНЕНО.");
                                return;
                            }

                            throw new Exception(response.StatusCode.ToString());
                        }
                    }
                    if (message.Text == "/gpt_date")
                    {
                        if (user.gptByDate)
                        {
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?gptD=false", null);
                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "ChatGPT для погоди по даті ВИМКНЕНО.");
                                return;
                            }
                            throw new Exception(response.StatusCode.ToString());

                        }
                        else
                        {
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?gptD=true", null);
                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "ChatGPT для погоди по даті УВІМКНЕНО.");
                                return;
                            }

                            throw new Exception(response.StatusCode.ToString());
                        }
                    }
                    if (message.Text == "/change_notification_days")
                    {
                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitD=true", null);
                        if (response.IsSuccessStatusCode)
                        {
                            await botClient.SendTextMessageAsync(id, "Введіть кількість днів");
                            return;
                        }

                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/change_notification_time")
                    {
                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitT=true", null);
                        if (response.IsSuccessStatusCode)
                        {
                            await botClient.SendTextMessageAsync(id, "Введіть час у форматі hh:mm");
                            return;
                        }

                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/change_notification_weather")
                    {
                        HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?waitW=true", null);
                        if (response.IsSuccessStatusCode)
                        {
                            await botClient.SendTextMessageAsync(id, "Будь ласка, введіть погоду про яку треба попереджати. Наприклад : Дощ, Сонячно");
                            return;
                        }

                        throw new Exception(response.StatusCode.ToString());
                    }
                    if (message.Text == "/check_notification_settings")
                    {
                        if (user.notification.weather == null || user.notification.days == 0)
                        {
                            await botClient.SendTextMessageAsync(id, "Спочатку налаштуйте нотифікацію!");
                            return;
                        }
;
                        await botClient.SendTextMessageAsync(id, $"Ваші налаштування нотифікації:" +
                            $"\nПогода->{user.notification.weather}" +
                            $"\nЧас оповіщення->{user.notification.notificationTime.Hour}:{user.notification.notificationTime.Minute}" +
                            $"\nКількість днів до погоди->{user.notification.days}");
                        return;
                    }
                    if (message.Text == "/notification")
                    {
                        if (user.notification.weather == null || user.notification.days == 0)
                        {
                            await botClient.SendTextMessageAsync(id, "Спочатку налаштуйте нотифікацію!");
                            return;
                        }
                        if (user.notification.notify)
                        {
                            user.notification.notify = false;
                            var json = JsonSerializer.Serialize(user.notification);
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?jsonNotification={json}", null);

                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "Нотифікацію ВИМКНЕНО.");
                                return;
                            }

                            throw new Exception(response.StatusCode.ToString());

                        }
                        else
                        {
                            user.notification.notify = true;
                            var json = JsonSerializer.Serialize(user.notification);
                            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?jsonNotification={json}", null);

                            if (response.IsSuccessStatusCode)
                            {
                                await botClient.SendTextMessageAsync(id, "Нотифікацію УВІМКНЕНО.");
                                return;
                            }

                            throw new Exception(response.StatusCode.ToString());
                        }
                    }
                }
            }

            await botClient.SendTextMessageAsync(id, "Нєма такого лол");
            return;
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR : {e.Message}");
            return;
        }
    }

    public static async Task Error(ITelegramBotClient botClient, Exception ex, CancellationToken token)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
    }
}