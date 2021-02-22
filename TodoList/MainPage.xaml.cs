using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TodoList.Model;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TodoList
{
    public sealed partial class MainPage : Page
    {
        ObservableCollection<ToDo> todos;
        ObservableCollection<string> DisplayOptions = new ObservableCollection<string>();
        SQLiteConnection db;
        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;

            SetDisplayOptions();

            var dbTask = DBConnection.connectAsync();
            db = dbTask.Result;
            db.CreateTable<ToDo>();

            RefreshTodoList();

            TodoListView.ItemsSource = todos;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            //timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += CheckForTodos;
            timer.Start();
        }

        private void CheckForTodos(object sender, object e)
        {
            string time = DateTime.Now.ToString("HH:mm");
            List<ToDo> tds = db.Table<ToDo>().OrderByDescending(t => t.BeginningDate).ToList();
            foreach (ToDo t in tds)
            {
                TimeSpan BeginDuration = t.BeginningDate - DateTime.Now;
                double BeginMinutes = BeginDuration.TotalMinutes;
                double EndMinutes = (t.EndingDate - DateTime.Now).TotalMinutes;

                if (BeginMinutes > 0 && BeginMinutes < 15 && !t.Done)
                {
                    string Title = "Start in " + BeginMinutes.ToString("0min : ") + t.Title;
                    string Content = "Start : " + FormatDateTime(t.BeginningDate) + "\nEnd  : " + FormatDateTime(t.EndingDate);
                    Content += "\n" + t.Content;
                    ShowToastNotification(Title, Content);
                }
                else if (EndMinutes > 0 && EndMinutes < 15 && !t.Done)
                {
                    string Title = "End in " + EndMinutes.ToString("0min : ") + t.Title;
                    string Content = "Start : " + FormatDateTime(t.BeginningDate) + "\nEnd  : " + FormatDateTime(t.EndingDate);
                    Content += "\n" + t.Content;
                    ShowToastNotification(Title, Content);
                }
            }
        }

        private string FormatDateTime(DateTime date)
        {
            return date.ToString("dd/MM/yyyy HH:mm");
        }

        private void ShowToastNotification(string title, string stringContent)
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }

        private void SetDisplayOptions()
        {
            DisplayOptions.Add("All");
            DisplayOptions.Add("Done");
            DisplayOptions.Add("In progress");
        }

        private void DoneCheckBoxChange(object sender, RoutedEventArgs e)
        {
            string idStr = ((CheckBox)sender).Tag.ToString();
            long id = Convert.ToInt64(idStr);

            ToDo todo = todos.SingleOrDefault(t => t.Id == id);
            todo.Done = (bool)((CheckBox)sender).IsChecked;

            db.Update(todo);
        }

        private void DeleteTodo(object sender, RoutedEventArgs e)
        {
            string idStr = ((Button)sender).Tag.ToString();
            long id = Convert.ToInt64(idStr);

            ToDo todo = todos.SingleOrDefault(t => t.Id == id);
            if (todo != null)
            {
                //todos.Remove(todo);
                db.Delete(todo);
                FilterTodosList();
            }

        }

        private void FilterTodoList(object sender, TextChangedEventArgs e)
        {
            FilterTodosList();
        }

        private void DisplayOptionsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTodosList();
        }

        private void TodoListDisplayDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            FilterTodosList();
        }

        private void ResetDate(object sender, RoutedEventArgs e)
        {
            TodoListDisplayDate.Date = null;
        }

        private void FilterTodosList()
        {
            RefreshTodoList();
            List<ToDo> FiltredTodos;
            FiltredTodos = todos.Where(todo => InSelectedDate(todo.BeginningDate) && IsSameState(todo)
                                                && todo.Title.Contains(FilterByTitle.Text, StringComparison.InvariantCultureIgnoreCase)).ToList();

            TodoListView.ItemsSource = FiltredTodos;
        }

        private void RefreshTodoList()
        {
            todos = new ObservableCollection<ToDo>(db.Table<ToDo>().OrderBy(t => t.BeginningDate).ToList());
        }

        private bool IsSameState(ToDo todo)
        {
            string selectedOption = (string)DisplayOptionsCB.SelectedItem;
            switch (selectedOption)
            {
                case "All": return true;
                case "Done": return todo.Done;
                case "In progress": return !todo.Done;
            }

            return true;
        }

        private bool InSelectedDate(DateTimeOffset TodoDate)
        {
            var date = TodoListDisplayDate.Date;

            if (date != null)
            {
                DateTime time = date.Value.DateTime;
                string FormatedDate = FormatDate(time);
                string FormatedTodoDate = FormatDate(TodoDate.DateTime);
                return FormatedDate.Equals(FormatedTodoDate);
            }
            return true;
        }

        private string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy");
        }

        private void ShowAddNewTodoPopup(object sender, RoutedEventArgs e)
        {
            AddNewTodoPopup.Height = 450;
            AddNewTodoPopup.Width = 300;
            AddNewTodoPopup.IsOpen = true;
        }

        private void CancelAddNewTodo(object sender, RoutedEventArgs e)
        {
            ClearFields();
            AddNewTodoPopup.IsOpen = false;
        }

        private void AddNewTodo(object sender, RoutedEventArgs e)
        {
            if (VerifyData())
            {
                ToDo NewTodo = new ToDo
                {
                    Title = AddNewTodoTitle.Text,
                    Content = AddNewTodoContent.Text,
                    BeginningDate = MergeTwoDateTime(AddNewTodoBeginningDate, AddNewTodoBeginningTime),
                    EndingDate = MergeTwoDateTime(AddNewTodoEndingDate, AddNewTodoEndingTime),
                    Done = false
                };

                //todos.Add(NewTodo);
                db.Insert(NewTodo);

                FilterTodosList();
                ClearFields();
                AddNewTodoPopup.IsOpen = false;
            }
        }

        private bool VerifyData()
        {
            PopupErrorMessage.Text = "* Please complete all fields";

            if ("".Equals(AddNewTodoTitle.Text))
            {
                SetErrorBorder(AddNewTodoTitle);
                return false;
            }
            else if (AddNewTodoBeginningDate.Date == null)
            {
                SetErrorBorder(AddNewTodoBeginningDate);
                return false;
            }
            else if (AddNewTodoBeginningTime == null || AddNewTodoBeginningTime.SelectedTime == null)
            {
                PopupErrorMessage.Visibility = Visibility.Visible;
                return false;
            }
            else if (AddNewTodoEndingDate.Date == null)
            {
                SetErrorBorder(AddNewTodoEndingDate);
                return false;
            }
            else if (AddNewTodoEndingTime == null || AddNewTodoEndingTime.SelectedTime == null)
            {
                PopupErrorMessage.Visibility = Visibility.Visible;
                return false;
            }
            else if ((MergeTwoDateTime(AddNewTodoEndingDate, AddNewTodoEndingTime) - MergeTwoDateTime(AddNewTodoBeginningDate, AddNewTodoBeginningTime))
                        .TotalMinutes < 0)
            {
                PopupErrorMessage.Text = "* Beginning date must be before Ending date";
                PopupErrorMessage.Visibility = Visibility.Visible;
                SetErrorBorder(AddNewTodoBeginningDate);
                SetErrorBorder(AddNewTodoEndingDate);
                return false;
            }

            PopupErrorMessage.Visibility = Visibility.Collapsed;
            return true;
        }

        private DateTime MergeTwoDateTime(CalendarDatePicker calendarDatePicker, TimePicker timePicker)
        {
            DateTime selectedDT = calendarDatePicker.Date.Value.DateTime;
            TimeSpan ts = timePicker.Time;
            return selectedDT.Date + ts;
        }

        private void ClearFields()
        {
            AddNewTodoTitle.Text = "";
            SetErrorBorder(AddNewTodoTitle, "none");
            AddNewTodoContent.Text = "";
            SetErrorBorder(AddNewTodoContent, "none");
            AddNewTodoBeginningDate.Date = null;
            SetErrorBorder(AddNewTodoBeginningDate, "none");
            AddNewTodoBeginningTime.SelectedTime = null;
            AddNewTodoEndingDate.Date = null;
            SetErrorBorder(AddNewTodoEndingDate, "none");
            AddNewTodoEndingTime.SelectedTime = null;

            PopupErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void SetErrorBorder(Control c, string type = "error")
        {
            if ("error".Equals(type))
                c.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
            else
                c.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);

            c.BorderThickness = new Thickness(2);
        }
    }
}
