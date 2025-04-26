using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProcessWatcher
{
    // !!!!! Все действия по работе с базой, которые в прошлой теме (когда не было MVVM) произодились в коде главного окна, теперь вынесены в команды в ApplicationViewModel
    public class ApplicationViewModel
    {
        ApplicationContext db = new ApplicationContext();
        RelayCommand? addCommand;
        RelayCommand? editCommand;
        RelayCommand? deleteCommand;
        public ObservableCollection<BannWord> BannWords { get; set; }
        public ObservableCollection<BannApp> BannApps { get; set; }
        public ApplicationViewModel()
        {
            db.Database.EnsureCreated();

            db.BannWords.Load();
            BannWords = db.BannWords.Local.ToObservableCollection();
            db.BannApps.Load();
            BannApps = db.BannApps.Local.ToObservableCollection();
        }
        // команда добавления в Базу данных
        public RelayCommand AddCommand
        {
            get
            {
                return addCommand ??
                  (addCommand = new RelayCommand((o) =>
                  {


                      // o будет содержать ссылку на элемент управления, вызвавший команду
                      var sourceElement = o as Button; // или другой тип элемента управления

                      // Например, можно получить имя кнопки, вызвавшей команду RelayCommand
                      string buttonName = sourceElement.Name;
                      //MessageBox.Show(buttonName);

                      // Вы можете использовать sourceElement для получения информации о кнопке
                      if (sourceElement != null)
                      {

                          if (buttonName == "AddLeft")
                          {
                              EnterBannApp userWindow = new EnterBannApp(new BannApp()); // открываем окно EnterBannApp
                              if (userWindow.ShowDialog() == true) // см. обработчик Accept_Click в EnterBannApp.xaml.cs
                              {
                                  BannApp appl = userWindow.BannApp;
                                  db.BannApps.Add(appl);
                                  db.SaveChanges();
                              }
                          }
                          else if (buttonName == "AddRight")
                          {

                              EnterBannWord userWindow = new EnterBannWord(new BannWord()); // открываем окно EnterBannWord
                              if (userWindow.ShowDialog() == true) // см. обработчик Accept_Click в EnterBannWord.xaml.cs
                              {
                                  BannWord word = userWindow.BannWord;
                                  db.BannWords.Add(word);
                                  db.SaveChanges();
                              }
                          }

                      }


                  }));
            }
        }

        // команда редактирования
        public RelayCommand EditCommand
        {
            get
            {
                return editCommand ??
                  (editCommand = new RelayCommand((selectedItem) => // selectedItem выбранный в списке объект
                  {
                      // получаем выделенный объект (знак вопроса как раз потому, что объект может быть и не выбран)
                      BannWord? word = selectedItem as BannWord;
                      BannApp? appl = selectedItem as BannApp;
                      if (word == null && appl == null) return;

                      if (word != null)
                      {

                          BannWord vm = new BannWord
                          {
                              Id = word.Id,
                              Word = word.Word  // Word - это свойство (не поле!!!), в котором прописано OnPropertyChanged("Word");
                          };
                          EnterBannWord userWindow = new EnterBannWord(vm);


                          if (userWindow.ShowDialog() == true)
                          {
                              word.Word = userWindow.BannWord.Word;
                              db.Entry(word).State = EntityState.Modified;
                              db.SaveChanges();
                          }
                      }

                      if (appl != null)
                      {

                          BannApp va = new BannApp
                          {
                              Id = appl.Id,
                              Appl = appl.Appl // Appl - это свойство (не поле!!!), в котором прописано OnPropertyChanged("Appl");
                          };
                          EnterBannApp userWindow = new EnterBannApp(va);


                          if (userWindow.ShowDialog() == true)
                          {
                              appl.Appl = userWindow.BannApp.Appl; ;
                              db.Entry(appl).State = EntityState.Modified;
                              db.SaveChanges();
                          }
                      }


                  }));
            }
        }
        // команда удаления
        public RelayCommand DeleteCommand
        {
            get
            {
                return deleteCommand ??
                  (deleteCommand = new RelayCommand((selectedItem) =>
                  {
                    // получаем выделенный объект (знак вопроса как раз потому, что объект может быть и не выбран)
                    BannWord? word = selectedItem as BannWord;
                    BannApp? appl = selectedItem as BannApp;
                    if (word == null && appl == null) return;

                    if (word != null)
                    {
                          db.BannWords.Remove(word);
                          db.SaveChanges();
                    }

                    if (appl != null)
                    {
                          db.BannApps.Remove(appl);
                          db.SaveChanges();
                    }

                  }));
            }
        }
    }

}
