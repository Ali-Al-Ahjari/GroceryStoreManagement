using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Xunit;
using GroceryStoreManagement.Windows;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.DAL;

namespace GroceryStoreManagement.Tests
{
    public class LoginWindowTests
    {
        [Fact]
        public void TestUserLoginAppDb()
        {
            string appDb = @"d:\D\SAM\3\نظام متكامل لادارة المتجر\GroceryStoreManagement\bin\Debug\net10.0-windows7.0\Data\GroceryStore.db";
            if (!System.IO.File.Exists(appDb))
            {
                throw new Exception("App database does not exist!");
            }
            using var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={appDb};Version=3;");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Username FROM Users";
            var reader = cmd.ExecuteReader();
            var list = new System.Collections.Generic.List<string>();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }
            throw new Exception($"AppDb users: [{string.Join(", ", list)}]");
        }

        [Fact]
        public void TestUserLoginDAL()
        {
            DatabaseHelper.InitializeDatabase();
            var user = UserDAL.GetUserByUsername("123");
            if (user == null)
            {
                var allUsers = UserDAL.GetAllUsers();
                var usernames = string.Join(", ", allUsers.Select(u => u.Username));
                throw new Exception($"User '123' not found in database! Existing users: [{usernames}]");
            }
        }

        [Fact]
        public void TestLoginWindowAndDialogsSequential()
        {
            Exception? threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // 1. Initialize Application singleton on this STA thread if not already created
                    if (Application.Current == null)
                    {
                        new Application();
                    }

                    // 2. Load Styles.xaml resources into the Application singleton
                    Application.Current.Resources.MergedDictionaries.Clear();
                    var stylesUri = new Uri("pack://application:,,,/GroceryStoreManagement;component/Styles/Styles.xaml", UriKind.Absolute);
                    var resourceDict = new ResourceDictionary { Source = stylesUri };
                    Application.Current.Resources.MergedDictionaries.Add(resourceDict);

                    // 3. Initialize Database
                    DatabaseHelper.InitializeDatabase();

                    // --- STEP A: Test Instantiation ---
                    var loginWindow = new LoginWindow();
                    Assert.NotNull(loginWindow);
                    loginWindow.Show();

                    var btnLicense = loginWindow.FindName("BtnLicenseActivation") as Button;
                    Assert.NotNull(btnLicense);

                    var btnSettings = loginWindow.FindName("BtnSettings") as Button;
                    Assert.NotNull(btnSettings);

                    var btnForgot = loginWindow.FindName("BtnForgotPassword") as Button;
                    Assert.NotNull(btnForgot);

                    var btnLogin = loginWindow.FindName("BtnLogin") as Button;
                    Assert.NotNull(btnLogin);


                    // --- STEP B: Test License Activation Window Open ---
                    bool licenseDialogDetected = false;
                    var timerLicense = new System.Windows.Threading.DispatcherTimer();
                    timerLicense.Interval = TimeSpan.FromMilliseconds(200);
                    timerLicense.Tick += (s, e) =>
                    {
                        foreach (Window w in loginWindow.OwnedWindows)
                        {
                            if (w is LicenseActivationWindow)
                            {
                                licenseDialogDetected = true;
                                w.Close();
                                timerLicense.Stop();
                                break;
                            }
                        }
                    };
                    timerLicense.Start();

                    btnLicense.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.True(licenseDialogDetected);


                    // --- STEP C: Test Connection Settings Window Open ---
                    bool settingsDialogDetected = false;
                    var timerSettings = new System.Windows.Threading.DispatcherTimer();
                    timerSettings.Interval = TimeSpan.FromMilliseconds(200);
                    timerSettings.Tick += (s, e) =>
                    {
                        foreach (Window w in loginWindow.OwnedWindows)
                        {
                            if (w is ConnectionSettingsDialog)
                            {
                                settingsDialogDetected = true;
                                w.Close();
                                timerSettings.Stop();
                                break;
                            }
                        }
                    };
                    timerSettings.Start();

                    btnSettings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.True(settingsDialogDetected);


                    // --- STEP D: Test Password Recovery Window Open ---
                    bool forgotDialogDetected = false;
                    var timerForgot = new System.Windows.Threading.DispatcherTimer();
                    timerForgot.Interval = TimeSpan.FromMilliseconds(200);
                    timerForgot.Tick += (s, e) =>
                    {
                        foreach (Window w in loginWindow.OwnedWindows)
                        {
                            if (w is PasswordRecoveryWindow)
                            {
                                forgotDialogDetected = true;
                                w.Close();
                                timerForgot.Stop();
                                break;
                            }
                        }
                    };
                    timerForgot.Start();

                    btnForgot.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.True(forgotDialogDetected);

                    loginWindow.Close();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
            {
                throw new Exception("Exception in STA thread: " + threadEx.ToString(), threadEx);
            }
        }
    }
}
