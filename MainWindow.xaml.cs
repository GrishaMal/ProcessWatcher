using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProcessWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProcessStartWatcher _processWatcher;
        private bool processFrameFlag = true;
        public ObservableCollection<ProcessInfo> Processes { get; set; }


        /*!!!!!!!!!!!!!!!!!!!!!!!!!!!Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
        // Поля для хранения имен файлов с учетом текущей даты и времени
        private readonly string LogFilePath;
        private readonly string LogFilePathAll;
        private readonly string LogFileBannWord;

        private bool UpdateResultsFlag = false;

        private Dictionary<string, int> wordCounts; // словарь, в котром будут храниться данные по числу введённых запрещённых слов
        private List<string> forbiddenWords; // словарь, в котром будут храниться данные по числу введённых запрещённых слов

        /* GetCurrentKeyboardLayout() - метод для определения языка раскладки */
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        public static string GetCurrentKeyboardLayout() // метод для определения языка раскладки. Вызывается в методе GetKeyPressed
        {
            IntPtr layout = GetKeyboardLayout(0);
            uint layoutId = (uint)layout.ToInt32();
            CultureInfo culture = new CultureInfo((int)(layoutId & 0xFFFF));
            return culture.Name; // Возвращает код языка (например, "en-US")
        }
        /*!!!!!!!!!!!!!!!!!!!!!!!!!!!Закрыли Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/

        public MainWindow()
        {
            InitializeComponent();
            //LoadProcesses();

            /*!!!!!!!!!!!!!!!!!!!!!!!!!!!Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
            // Генерация имен файлов с учетом текущей даты и времени только один раз при запуске приложения
            string timestamp = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss");
            LogFilePath = $"KeyLogger/keylog_{timestamp}.txt";
            LogFilePathAll = $"KeyLogger/Allkeylog_{timestamp}.txt";
            LogFileBannWord = $"KeyLogger/BannWordlog_{timestamp}.txt";

            // InputRichTextBox - это поле Name элемента RichTextBox на MainWindow.xaml
            InputRichTextBox.KeyDown += InputRichTextBox_KeyDown; // Добавляем обработчик нажатия на клавишу
            InitializeWordCounts();  // Инициализируем счётчики для каждого слова
            UpdateResults(); // Метод UpdateResults() обновляет интерфейс с текущими значениями счётчиков
            /*!!!!!!!!!!!!!!!!!!!!!!!!!!!Закрыли Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/

            Processes = new ObservableCollection<ProcessInfo>();
            ProcessListView.ItemsSource = Processes;

            _processWatcher = new ProcessStartWatcher(Processes);
            _processWatcher.StartWatching();

        }

        protected override void OnClosed(EventArgs e) // OnClosed: При закрытии окна мы останавливаем и освобождаем ресурсы наблюдателя
        {
            // Останавливаем наблюдатель при закрытии окна
            _processWatcher.StopWatching();
            base.OnClosed(e);
        }

        // В этом методе мы получаем все запущенные процессы с помощью Process.GetProcesses(). Мы фильтруем процессы,
        // чтобы оставить только те, у которых есть главное окно (это делается с помощью свойства MainWindowTitle).
        private void LoadProcesses()
        {
            // Получаем список всех запущенных процессов
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)) // Фильтруем только те процессы, у которых есть окно
                .Select(p => new ProcessInfo
                {
                    Name = p.ProcessName,
                    StartTime = p.StartTime.ToString("G") // Форматируем время запуска
                })
                .ToList();

            // Устанавливаем источник данных для ListView
            ProcessListView.ItemsSource = processes;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Получаем ссылку на выбранный RadioButton
            RadioButton selectedRadioButton = sender as RadioButton;
            if (selectedRadioButton.Content.ToString() == "Только оконные") _processWatcher.SetFlag = true;
            else _processWatcher.SetFlag = false;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {

            Banned_Word_App Banned_Word = new Banned_Word_App();
            Banned_Word.Show(); // Открывает окно для ввода запрещённых слов и приложений

        }

        private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Получаем выбранный элемент
            ComboBoxItem selectedItem = (ComboBoxItem)MyComboBox.SelectedItem;

            if (selectedItem != null)
            {
                // Получаем текст выбранного элемента
                // string selectedText = selectedItem.Content.ToString();

                if (selectedItem.Content.ToString() == "Модерирование") _processWatcher.SetFlagModer = true;
                else _processWatcher.SetFlagModer = false;


            }
        }







        /*!!!!!!!!!!!!!!!!!!!!!!!!!!! Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/







        // Метод "InputRichTextBox_TextChanged" прописан в поле TextChanged элемента RichTextBox главного окна
        // InputRichTextBox_TextChanged вызываться каждый раз, когда текст в RichTextBox изменяется
        private void InputRichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // все три метода ниже вызываются при срабатывании InputRichTextBox_TextChanged
            CountForbiddenWords();  // метод для подсчётча запрещённых слов: вызывается при срабатывании InputRichTextBox_TextChanged
            UpdateResults(); // обновляет интерфейс с текущими значениями счётчиков
            LogWords(); // перезаписываем файл "keylog.txt" (пишем туда только слова и знаки препинания)
        }


        // На этот метод ссылается делегат, прописанный в InitializeComponent. Вот сам делегат: InputRichTextBox.KeyDown, где InputRichTextBox - это поле Name элемента RichTextBox на MainWindow.xaml
        private void InputRichTextBox_KeyDown(object sender, KeyEventArgs e) // sender - клавиша-отправитель события, KeyEventArgs - событие нажатия на клавишу
        {

            //char keyChar = (char)KeyInterop.VirtualKeyFromKey(e.Key);
            //MessageBox.Show(keyChar.ToString());

            // Получаем фактический символ, который был введен
            string keyPressed = GetKeyPressed(e); // см. этот метод ниже

            // Запись нажатой клавиши в файл. Добавляем запись в конец файла, не перезаписывая его
            if (e.Key == Key.Enter)
            {
                File.AppendAllText(LogFilePathAll, "Нажата клавиша: Enter" + Environment.NewLine);
            }
            else if (e.Key == Key.Space)
            {
                File.AppendAllText(LogFilePathAll, "Нажата клавиша: Space" + Environment.NewLine);
            }
            else if (e.Key == Key.Back)
            {
                File.AppendAllText(LogFilePathAll, "Нажата клавиша: BackSpace" + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(LogFilePathAll, keyPressed + Environment.NewLine); // keyPressed - см. выше
            }
        }

        private string GetKeyPressed(KeyEventArgs e)
        {
            string currentLayout = GetCurrentKeyboardLayout(); // это потребуется для определения раскладки (русская или нет)

            // Получаем фактический символ, который был введен
            char keyChar = (char)KeyInterop.VirtualKeyFromKey(e.Key);
            // Преобразуем символ в строку
            string input = keyChar.ToString();
            bool num_flag = int.TryParse(input, out _); // num_flag того, что введённый символ является числом

            // Проверяем, является ли строка числом
            if (num_flag)
            {
                // Обработка других клавиш (например, цифры)
                switch (e.Key)
                {
                    case Key.D0: return "0";
                    case Key.D1: return "1";
                    case Key.D2: return "2";
                    case Key.D3: return "3";
                    case Key.D4: return "4";
                    case Key.D5: return "5";
                    case Key.D6: return "6";
                    case Key.D7: return "7";
                    case Key.D8: return "8";
                    case Key.D9: return "9";

                    // Добавьте обработку других клавиш по мере необходимости

                    default:
                        return e.Key.ToString(); // так как уже определилили, что ввели число, то этот код никогда не выполнится
                }

            }

            // Проверяем, является ли раскладка русской и при этом введено НЕ число
            if (!currentLayout.StartsWith("en") && !num_flag)
            {
                switch (e.Key)
                {
                    case Key.Q: return "Й";
                    case Key.W: return "Ц";
                    case Key.E: return "У";
                    case Key.R: return "К";
                    case Key.T: return "Е";
                    case Key.Y: return "Н";
                    case Key.U: return "Г";
                    case Key.I: return "Ш";
                    case Key.O: return "Щ";
                    case Key.P: return "З";
                    case Key.OemOpenBrackets: return "Х";
                    case Key.Oem6: return "Ъ";
                    case Key.A: return "Ф";
                    case Key.S: return "Ы";
                    case Key.D: return "В";
                    case Key.F: return "А";
                    case Key.G: return "П";
                    case Key.H: return "Р";
                    case Key.J: return "О";
                    case Key.K: return "Л";
                    case Key.L: return "Д";
                    case Key.Oem1: return "Ж";
                    case Key.OemQuotes: return "Э";
                    case Key.Z: return "Я";
                    case Key.X: return "Ч";
                    case Key.C: return "С";
                    case Key.V: return "М";
                    case Key.B: return "И";
                    case Key.N: return "Т";
                    case Key.M: return "Ь";
                    case Key.OemComma: return "Б";
                    case Key.OemPeriod: return "Ю";

                    // Добавьте обработку других клавиш по мере необходимости

                    default:
                        return e.Key.ToString(); // Возвращаем название клавиши по умолчанию // e.Key.ToString() - название клавиши
                }

            }

            return e.Key.ToString();  // e.Key.ToString() - название клавиши

        }

        private void LogWords() // метод, осуществляющий логгирование слов и знаков препинания (пробелы и Enter игнорируются)
        {
            var textRange = new TextRange(InputRichTextBox.Document.ContentStart, InputRichTextBox.Document.ContentEnd);
            var text = textRange.Text; // сюда заносится весь текст в окне InputRichTextBox (это поле Name элемента RichTextBox на MainWindow.xaml)

            // Разделяем текст на слова по пробелам и другим разделителям
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Записываем каждое слово в файл слитно в строку. В данном случае файл постоянно перезаписывается.
            // Это связано с тем, что всякий раз, при каждом новом вводе символа, в переменную text записывется ВЕСЬ текст
            // из окна (в том числе и уже ранее набранный текст)
            File.WriteAllLines(LogFilePath, words);
        }



        private void CountForbiddenWords() // счётчик запрещённых слов (вызывается в InputRichTextBox_TextChanged)
        {
            var textRange = new TextRange(InputRichTextBox.Document.ContentStart, InputRichTextBox.Document.ContentEnd);
            var text = textRange.Text; // Получаем весь текст из RichTextBox

            List<string> forbiddenWords = forbiddenWordsFun(); // Инициализация списка

            // этот флаг задаётся в методе InitializeWordCounts
            if (UpdateResultsFlag) { UpdateResultsFlag = false; InitializeWordCounts(); } // флаг важен в том случае, если при запуске программы в базе не было запрещённых слов
            // Сбрасываем счётчики перед новым подсчётом
            foreach (var word in forbiddenWords)  // forbiddenWords - Список запрещённых слов
            {
                wordCounts[word] = 0;
            }

            // Подсчитываем количество вхождений каждого запрещённого слова
            foreach (var word in forbiddenWords)  // forbiddenWords - Список запрещённых слов
            {
                int startIndex = 0;
                // text.IndexOf(word, startIndex) вернет индекс первого вхождения слова word в строке text. Причём поиск начинается с индекса startIndex
                while ((startIndex = text.IndexOf(word, startIndex)) != -1) // если не найдёно вхождений слова word в строке text, то метод text.IndexOf вернёт -1
                {
                    wordCounts[word]++;
                    startIndex += word.Length; // Продвигаем индекс для поиска следующего вхождения
                }
            }
        }

        private void UpdateResults() // Метод UpdateResults() обновляет интерфейс с текущими значениями счётчиков
        {
            ResultStackPanel.Children.Clear(); // Очищаем предыдущие результаты. ResultStackPanel - это значение поля Name в StackPanel

            var bannWord = new StringBuilder(); // Используем StringBuilder для более эффективной конкатенации строк

            if (wordCounts == null) return; //  если при запуске программы в базе не было запрещённых слов

            // этот флаг задаётся в методе InitializeWordCounts
            if (UpdateResultsFlag) { UpdateResultsFlag = false; InitializeWordCounts(); } // флаг важен в том случае, если при запуске программы в базе не было запрещённых слов

            foreach (var kvp in wordCounts)  // wordCounts - словарь, в котром хранятся данные по числу введённых запрещённых слов
            {
                bannWord.AppendLine($"{kvp.Key}: {kvp.Value}"); // Добавляем строку к bannWord класса StringBuilder
                var resultTextBlock = new TextBlock // создаём экземпляр класса TextBlock
                {
                    // Text и Foreground - поля класса TextBlock
                    Text = $"{kvp.Key}: {kvp.Value}",
                    Foreground = Brushes.Black, // Устанавливаем цвет текста на черный
                    FontSize = 20
            };
                ResultStackPanel.Children.Add(resultTextBlock); // Добавляем результат в StackPanel (см. главное окно)
            }
            // Записываем содержимое bannWord в файл
            File.WriteAllText(LogFileBannWord, bannWord.ToString());
        }


        private void InitializeWordCounts() // Инициализируем счётчики для каждого слова
        {
            try
            {
                // Загрузка БД
                using (ApplicationContext db = new ApplicationContext()) // using автоматически вызовет очистку памяти
                {
                    var words = db.BannWords.ToList(); // Получаем список запрещенных слов

                    if (words.Count == 0)
                    {
                        MessageBox.Show("Список запрещенных слов пуст. Заполните его!!!");
                        UpdateResultsFlag = true; // этот флаг используется в методах CountForbiddenWords и UpdateResults
                        return;
                    }

                    wordCounts = new Dictionary<string, int>();
                    foreach (BannWord word in words) // Проходим по списку всех запрещённых слов
                    {
                        wordCounts[word.Word] = 0; // Инициализируем счётчики для каждого слова
                    }
                }
            }
            catch (Exception ex) // Обработка исключений
            {
                MessageBox.Show($"Произошла ошибка при инициализации счетчиков слов: {ex.Message}");
            }
        }


        private List<string> forbiddenWordsFun()   // Инициализируем счётчики для каждого слова
        {

            // Загрузка БД
            using (ApplicationContext db = new ApplicationContext()) // using автоматически вызовет очистку памяти
            {

                var words = db.BannWords.ToList(); // Без подключения using System.Linq; будет ошибка
                forbiddenWords = new List<string>();

                if (words.Count == 0)
                {
                    MessageBox.Show("Список запрещенных слов пуст. Заполните его!!!");
                    return forbiddenWords;
                }
                
                foreach (BannWord word in words) // двигаемся по списку всех запрещённых слов
                {
                    forbiddenWords.Add(word.Word); // Инициализируем счётчики для каждого слова
                }
                return forbiddenWords;
            }

        }


        /*!!!!!!!!!!!!!!!!!!!!!!!!!!! Закрыли Блок для работы со словами !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/


    }

    public class ProcessInfo
    {
        public string Name { get; set; }
        public string StartTime { get; set; }
    }
}
