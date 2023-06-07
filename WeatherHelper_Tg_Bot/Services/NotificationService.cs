using System.Timers;
using Telegram.Bot;
using WeatherAPI.Models;

namespace WeatherHelperTGBOT.Services
{
    public class NotificationService
    {
        private System.Timers.Timer timer;
        private ITelegramBotClient botClient;
        static BotApiService botService = new BotApiService();

        public NotificationService(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
        }
        public void Start()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += TimerElapsed;
            timer.Interval = 60000 * 2;
            timer.Start();
        }
        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                TimeZoneInfo gmtPlus3TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");

                TimeSpan currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, gmtPlus3TimeZone).AddHours(3).TimeOfDay;

                List<UserData> users = await botService.GetUsersToNotify();

                if (users == null || users.Count == 0) { return; }

                foreach (var user in users)
                {
                    bool notification = false;
                    long chatId = user.chatId;

                    TimeSpan notificationTime = user.notification.notificationTime.TimeOfDay;

                    if (currentTime.Hours == notificationTime.Hours)
                    {
                        if (currentTime.Minutes <= notificationTime.Minutes + 2 && currentTime.Minutes > notificationTime.Minutes)
                        {
                            notification = true;
                        }
                    }
                    else if (notificationTime.Minutes + 2 >= 60)
                    {
                        if (currentTime.Minutes <= (notificationTime.Minutes + 2) % 10)
                        {
                            notification = true;
                        }
                    }

                    if (notification)
                    {
                        await botClient.SendTextMessageAsync(chatId, "|||||Ваше сповіщення про погоду|||||");

                        if (await botService.GetDayByWeather(botClient, user.chatId, user.location, user.notification.weather) == null)
                        {
                            await botClient.SendTextMessageAsync(user.chatId, $"Дня в діапазоні {user.notification.days} днів з погодою {user.notification.weather} немає :)");
                        }

                        await botClient.SendTextMessageAsync(chatId, "|||||Ваше сповіщення про погоду|||||");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : {ex.Message} in TimerElapsed");
                return;
            }
        }
    }
}
