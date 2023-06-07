using System.Net;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using WeatherAPI.Models;
using WeatherHelperTGBOT.Constant;
using WeatherHelperTGBOT.Models;

namespace WeatherHelperTGBOT.Services
{
    public class BotApiService
    {
        static HttpClient httpClient;
        public BotApiService()
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            };
            httpClient = new HttpClient(socketsHandler);
        }
        public async Task<IAsyncResult> PatchNotification(Notification notification, Message message)
        {
            var json = JsonSerializer.Serialize(notification);
            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?jsonNotification={json}", null);

            if (response.IsSuccessStatusCode)
            {
                return Task.CompletedTask;
            }

            throw new Exception(response.StatusCode.ToString());
        }
        public async Task<IAsyncResult> CreateUser(Message message)
        {
            HttpResponseMessage response = await httpClient.PostAsync(Constants.url + $"/users/create?id={message.Chat.Id}", null);
            if (response.IsSuccessStatusCode)
            {
                return Task.CompletedTask;
            }
            else
                throw new Exception(response.StatusCode.ToString());
        }
        public async Task<IAsyncResult> AddLocation(Message message, bool cord = false)
        {
            string location;
            if (cord)
            {
                location = addCords(message);
            }
            else
            {
                location = message.Text;
            }
            HttpResponseMessage response = await httpClient.PatchAsync(Constants.url + $"/users/{message.Chat.Id}?loc={location}", null);

            if (response.IsSuccessStatusCode)
            {
                return Task.CompletedTask;
            }
            throw new Exception("Patching error");
        }
        private static string addCords(Message message)
        {
            double lat = message.Location.Latitude;
            double lon = message.Location.Longitude;

            Console.WriteLine($"{message.Chat.Username ?? $"{message.Chat.FirstName}"}  |   {lat},{lon}");

            return $"{lat.ToString().Replace(',', '.')},{lon.ToString().Replace(',', '.')}";
        }
        public async Task<IAsyncResult> GetCurrentWeather(ITelegramBotClient botClient, Message message, string location, bool gpt)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + @"/weather/current/" + location);

            if (response.IsSuccessStatusCode)
            {
                CurrentWeatherData responseContent = await JsonSerializer.DeserializeAsync<CurrentWeatherData>(response.Content.ReadAsStream());
                await botClient.SendTextMessageAsync(message.Chat.Id, $"{responseContent.current.condition.text}\n{responseContent.location.country}, {responseContent.location.name}\n{responseContent.location.localtime}\nТемпература-> {responseContent.current.temp_c}°C\nВідчувається на-> {responseContent.current.feelslike_c}°C\nВітер-> {responseContent.current.wind_kph}км/г\nВологість-> {responseContent.current.humidity}%\nОпади-> {responseContent.current.precip_mm}мм");

                if (gpt)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Очікуємо на відповідь від ChatGPT...");

                    HttpResponseMessage responseGpt = await httpClient.GetAsync(Constants.url + @"/weather/gpt/" + location + "?c=1");

                    if (response.IsSuccessStatusCode)
                    {
                        string gptAnswer = await responseGpt.Content.ReadAsStringAsync();

                        await botClient.SendTextMessageAsync(message.Chat.Id, gptAnswer + "\n\n\nChatGPT");
                    }
                    else
                        await botClient.SendTextMessageAsync(message.Chat.Id, "ChatGPT не працює :(");
                }

                return Task.CompletedTask;
            }
            throw new Exception(response.StatusCode.ToString());
        }
        public async Task<IAsyncResult> GetForecast(ITelegramBotClient botClient, Message message, string location, int days)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + @"/weather/forecast/" + location + "/" + days);

            if (response.IsSuccessStatusCode)
            {
                ForecastWeatherData responseContent = await JsonSerializer.DeserializeAsync<ForecastWeatherData>(response.Content.ReadAsStream());
                foreach (var day in responseContent.forecast.forecastday)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, FormStringForDay(day, responseContent.location));
                }
                return Task.CompletedTask;
            }
            throw new Exception(response.StatusCode.ToString());
        }
        private string FormStringForDay(Forecastday forecastday, Models.Location location)
        {
            string forecastString = "";
            var day = forecastday.day;
            var date = forecastday.date;
            var hours = forecastday.hour;

            forecastString += $"{location.country}, {location.name}\n";

            forecastString += $"{date}\n";

            forecastString += "Загальний прогноз на день:\n";

            forecastString += $"" +
                $"\t{day.condition.text}" +
                $"\n\tТемпература->{day.avgtemp_c}°C" +
                $"\n\t\tМаксимальна->{day.maxtemp_c}°C" +
                $"\n\t\tМінімальна->{day.mintemp_c}°C" +
                $"\n\t";

            forecastString += $"" +
                $"Шанс дощу->{day.daily_chance_of_rain}%" +
                $"\n\tВологість->{day.avghumidity}%" +
                $"\n\n";
            foreach (var hour in hours)
            {
                forecastString += $"||||||||||{hour.time}||||||||||" +
                    $"\n{hour.condition.text}\nТемпература-> {hour.temp_c}°C" +
                    $"\n\tВідчувається на-> {hour.feelslike_c}°C" +
                    $"\nВітер-> {hour.wind_kph}км/г" +
                    $"\nВологість-> {hour.humidity}%" +
                    $"\nОпади->{hour.precip_mm}мм" +
                    $"\nШанс дощу-> {hour.chance_of_rain}%\n\n";
            }
            return forecastString;
        }
        public async Task<UserData> GetUserData(Message message)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + $"/users/{message.Chat.Id}");
            if (response.IsSuccessStatusCode)
            {
                UserData responseContent = await JsonSerializer.DeserializeAsync<UserData>(response.Content.ReadAsStream());
                return responseContent;
            }
            if (response.StatusCode.Equals(HttpStatusCode.NotFound))
            {
                return null;
            }
            throw new Exception(response.StatusCode.ToString());
        }
        public async Task<List<UserData>> GetUsersToNotify()
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + $"/users/notify");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await JsonSerializer.DeserializeAsync<List<UserData>>(response.Content.ReadAsStream());
                return responseContent;
            }
            else
                throw new Exception(response.StatusCode.ToString());
        }
        public async Task<IAsyncResult> GetWeatherByDate(ITelegramBotClient botClient, Message message, string location, string date, bool gpt)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + @"/weather/date/" + location + "/" + date);

            if (response.IsSuccessStatusCode)
            {
                ForecastWeatherData responseContent = await JsonSerializer.DeserializeAsync<ForecastWeatherData>(response.Content.ReadAsStream());
                foreach (var day in responseContent.forecast.forecastday)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, FormStringForDay(day, responseContent.location));
                }

                if (gpt)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Очікуємо на відповідь від ChatGPT...");

                    response = await httpClient.GetAsync(Constants.url + @"/weather/gpt/" + location + $"?date={date}");

                    if (response.IsSuccessStatusCode)
                    {
                        string gptAnswer = await response.Content.ReadAsStringAsync();

                        await botClient.SendTextMessageAsync(message.Chat.Id, gptAnswer + "\n\n\nChatGPT");
                    }
                    else
                        await botClient.SendTextMessageAsync(message.Chat.Id, "ChatGPT не працює :(");
                }

                return Task.CompletedTask;
            }
            return null;
        }
        public async Task<IAsyncResult> GetDayByWeather(ITelegramBotClient botClient, long id, string location, string weather, int days = 14)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Constants.url + @"/weather/find/" + location + "/" + weather + "?days=" + days);

            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                ForecastWeatherData responseContent = await JsonSerializer.DeserializeAsync<ForecastWeatherData>(response.Content.ReadAsStream());

                if (responseContent != null)
                {
                    foreach (var day in responseContent.forecast.forecastday)
                    {
                        await botClient.SendTextMessageAsync(id, FormStringForDay(day, responseContent.location));
                    }
                    return Task.CompletedTask;
                }
                else
                    return null;
            }
            if (response.StatusCode.Equals(HttpStatusCode.NoContent))
                return null;
            throw new Exception(response.StatusCode.ToString() + " GetDayByWeather");
        }
    }
}
