using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Windows;
using System.Xml.Linq;

namespace ProcessWatcher
{
    public class ProcessStartWatcher // здесь заложена основная логика по отслеживанию запуска приложений и их блокировке
    {

        // Поля для хранения имен файлов с учетом текущей даты и времени
        private readonly string ProcessKillFile;

        // создаём экземпляр класса ManagementEventWatcher, который будет отслеживать события запуска процессов с помощью запроса WMI(Win32_ProcessStartTrace).
        private ManagementEventWatcher _watcher; // есть ещё ManagementObjectSearcher

        private bool _processFrameFlag = true;
        static bool _processModerir = true;

        public bool SetFlag
        {
            get { return _processFrameFlag; }
            set { _processFrameFlag = value; }
        }

        public bool SetFlagModer
        {
            get { return _processModerir; }
            set { _processModerir = value; }
        }

        public ObservableCollection<ProcessInfo> Processes { get; private set; }

        // !!!!!!!! В C# вы не можете инициализировать поля класса напрямую в теле класса, если они не являются статическими.
        // Вам нужно использовать конструктор для инициализации полей.
        public ProcessStartWatcher(ObservableCollection<ProcessInfo> processes)
        {
            Processes = processes;
            // Генерация имен файлов с учетом текущей даты и времени только один раз при запуске приложения
            string timestamp = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss");
            ProcessKillFile = $"KillProcess/ProcessKill_{timestamp}.txt";
        }

        public void StartWatching()
        {
            // Создаем запрос на отслеживание событий запуска процессов
            WqlEventQuery query = new WqlEventQuery(
                "SELECT * FROM Win32_ProcessStartTrace");

            _watcher = new ManagementEventWatcher(query);
            _watcher.EventArrived += new EventArrivedEventHandler(OnProcessStarted); // метод OnProcessStarted вызывается при запуске нового приложения
            _watcher.Start();
        }

        // OnProcessStarted: Этот метод вызывается каждый раз, когда происходит событие запуска процесса. Мы получаем имя процесса
        // и время его создания и добавляем новую запись в коллекцию Processes. Обратите внимание на использование
        // Dispatcher.Invoke, чтобы обновить коллекцию из потока события
        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            // Получаем информацию о процессе
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            string processId = e.NewEvent.Properties["ProcessID"].Value.ToString();
            //var startTime = ManagementDateTimeConverter.ToDateTime(e.NewEvent["StartTime"].ToString());

            if (!string.IsNullOrEmpty(processName) && !string.IsNullOrEmpty(processId))
            {
                // Проверяем, является ли процесс оконным
                if (IsWindowedApplication(processId))
                {

                    // MessageBox.Show(selectedRadioButton.Content.ToString());
                    Process process = Process.GetProcessById(int.Parse(processId));
                    // Добавляем новую запись в коллекцию
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Processes.Add(new ProcessInfo
                        {
                            Name = processName,
                            //StartTime = startTime.ToString("G") // Форматируем время запуска
                            StartTime = process.StartTime.ToString("G")
                        });
                    });

                    string[] words = processName.Split('.'); // слабое место, так как могут быть имена приложений, содержащих точку уже то .exe, например Rider.Backend.exe

                    if (ProcessBanned(words[0]) && _processModerir) {  // проверяем, является ли запущенное оконное приложение запрещённым

                        File.AppendAllText(ProcessKillFile, processName + ", " + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Environment.NewLine);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Processes.Add(new ProcessInfo
                            {
                                Name = processName,
                                //StartTime = startTime.ToString("G") // Форматируем время запуска
                                StartTime = "KillProcess"
                            });
                        });
                    }


                }
                else if (!_processFrameFlag) {


                    try // это необходимо так как Процесс с processId мог завершиться до того, как мы попытались получить его. Это довольно распространенная ситуация
                    {
                        // Проверяем все процессы
                        Process[] processes = Process.GetProcesses();
                        bool processExists = false;
                        DateTime startTime;
                        startTime = DateTime.MinValue;

                        foreach (var process in processes)
                        {
                            if (process.Id == int.Parse(processId))
                            {
                                processExists = true;
                                break;
                            }
                        }

                        if (processExists)
                        {
                            // Получаем процесс по его ID
                            Process process = Process.GetProcessById(int.Parse(processId));

                            // Получаем время начала процесса
                            startTime = process.StartTime;

                            // Добавляем новую запись в коллекцию
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                Processes.Add(new ProcessInfo
                                {
                                    Name = processName,
                                    //StartTime = startTime.ToString("G") // Форматируем время запуска
                                    StartTime = startTime != DateTime.MinValue ? startTime.ToString("G") : "Неизвестно" // Проверяем на инициализацию
                                });
                            });

                        }
                        else
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                Processes.Add(new ProcessInfo
                                {
                                    Name = processName,
                                    //StartTime = startTime.ToString("G") // Форматируем время запуска
                                    StartTime = startTime != DateTime.MinValue ? startTime.ToString("G") : "Неизвестно" // Проверяем на инициализацию
                                });
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Processes.Add(new ProcessInfo
                            {
                                Name = processName
                            });
                        });
                    }



                } // закрыли else if (!_processFrameFlag) 
            }

            
        }

        
        public void StopWatching()
        {
        // Останавливаем наблюдатель
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher.Dispose();
                _watcher = null;
            }
        }


        private static bool IsWindowedApplication(string processId)
        {
            try
            {
                int id = int.Parse(processId);
                Process process = Process.GetProcessById(id);
                string processName = process.ProcessName;
                //MessageBox.Show(processName);

                // Проверяем, есть ли у процесса окно
                return process.MainWindowHandle != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Ошибка при проверке процесса: {ex.Message}");
                return false;
            }
        }

        // будем использовать метод ProcessBanned только для оконных приложений
        static bool ProcessBanned(string processName) // Проверяем, есть ли процесс с именем processName в списке запрещённых. Если да, то убиваем процесс 
        {

            // Загрузка БД
            using (ApplicationContext db = new ApplicationContext()) // using автоматически вызовет очистку памяти
            {

                // Убедитесь, что база данных создана
                //db.Database.EnsureCreated();
                // Загрузка данных из базы
                //db.BannApps.Load(); // Загружаем данные из таблицы BannApps

                var apps = db.BannApps.ToList(); // Без подключения using System.Linq; будет ошибка

                if (apps.Count == 0)
                {
                    MessageBox.Show("Список запрещенных приложений пуст.");
                    return false; // Или другое значение по вашему усмотрению
                }

                foreach (BannApp appl in apps) // двигаемся по списку всех запрещённых приложений
                {

                    if (appl.Appl == processName) 
                    {
                        try
                        {
                            // Получаем все процессы с указанным именем
                            Process[] processes = Process.GetProcessesByName(processName);

                            // Проверяем, есть ли такие процессы
                            if (processes.Length > 0)
                            {
                                foreach (var process in processes)
                                {
                                    // Завершаем процесс
                                    if(_processModerir) process.Kill();
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка: {ex.Message}");
                            return false;
                        }

                    }
                }
            }

            return false; // Если ни одно из условий не выполнено, возвращаем false по умолчанию

        }




    }

}
