using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Application = Autodesk.Revit.ApplicationServices.Application;
using Form = System.Windows.Forms.Form;

namespace Room_Decoration
{
    public partial class Form1 : Form
    {
        /*                               Начать с
                                       1 - задачи   

      -                                   Задачи
          * 1. Скорректировать Высоту Отделки  -> Выполнена
          * 2. Фиксить поблему с Отсоединением стен во время генерации отделки в стенах с дверьми и другие Caution месседжи - пока Отложена
          * 3. Фиксить Баг со вложенным листом listOfListRooms - Выполнена
          * 4. Генерировать различные Отделки (типов стены) для разных комнат в соответсвие с паметром отделки комнат
          * 5. Улучшить обработку ошибок пользователя

        */

        /*                Добавленные фичи и изменение
           *                           21.10
             - Предусмотрено несколько вариантов возникновение ошибок пользователя
                Защита от лишных пробелов 
             - Добавлено автозаполнение для поле "Значений" 
             - Включена Уровень в селектор
           *
                                       24.10
             - Исправлена пара багов
             - Добавлена новая фича "Содержить" (в Условиях)

           *                           31.10
             - Добавлено вложенный список для группирование комнат по параметрам отделки стены
                Есть баг - во вложенный список попадает лишный элемент, потом исправить

          *                             1.11
             - Исправлен баг - со вложенными списками
          */

        #region Поля
        public UIDocument uidoc;
        public static Document doc;
        public UIApplication uiapp;
        public Application app;
        public List<string> listComboValue = new List<string>(); // Список для хранение и передачи результатов к ComboValue
        public List<string> listDecorType = new List<string>(); // Список для хранение и передачи результатов к ComboValue

        public ExternalEventClass myExternalClass;
        public ExternalEvent ExEvent;


        List<List<Room>> listOfListRooms = new List<List<Room>>(); // Вложенный - материнский  список
        List<Room> listOfRooms = new List<Room>(); // Список для хранение и отображения результатов
        #endregion


        // Получение необходимого параметра ExternalCommandData из Command.cs
        public Form1(ExternalCommandData commandData)
        {
            InitializeComponent();
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            app = uiapp.Application;
            doc = uidoc.Document;

            myExternalClass = new ExternalEventClass(commandData, listOfRooms);
            ExEvent = ExternalEvent.Create(myExternalClass);

        }

        ////

        // Заполнение список параметров и список условий
        private void Form1_Load(object sender, EventArgs e) // Загрузка формы
        {
            // Получаю экземпляр первой комнате в документе для заполнение список параметров
            Element roomForParam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().FirstElement();
            // Заполняю список параметров
            foreach (Parameter p in roomForParam.ParametersMap)
            {
                comboParameters.Items.Add(p.Definition.Name);
            }
            // Приведу в порядок значений и внешний вид списка параметров
            comboParameters.Sorted = true;
            //comboParameters.SelectedIndex = 0; // По умолчанию отображается первый элемент (0) из списка в поле ввода 
            comboParameters.DropDownStyle = ComboBoxStyle.DropDownList; // Отключаю ввод пользователя (он только может выбрать)            
            comboLevel.DropDownStyle = ComboBoxStyle.DropDownList; // Верхнаое действие для списка уровней           
            comboValue.AutoCompleteMode = AutoCompleteMode.SuggestAppend; // Автозаполнение для поле значений          
            comboValue.AutoCompleteSource = AutoCompleteSource.ListItems; // Автозаполнение для поле значений          
            // Заполняю список условий
            comboCondition.Items.Add("равно");
            comboCondition.Items.Add("не равно");
            comboCondition.Items.Add("содержит");
            // Приведу в порядок значений и внешний вид списка параметров
            //comboCondition.SelectedIndex = 0;
            comboCondition.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        ////

        // Заполнение список уровней
        private void comboLevel_Enter(object sender, EventArgs e) // Когда comboLevel (Выпадающий Список Уровней) активируется мышю или клавиатурой
        {
            comboLevel.Items.Clear();
            List<string> listComboLevel = new List<string>(); // Список для хранение и передачи результатов к ComboValue
            FilteredElementCollector rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType();
            foreach (Room item in rooms)
            {
                foreach (Parameter p in item.ParametersMap)
                {
                    if ((p.Definition.Name == "Уровень" && p.StorageType == StorageType.ElementId))
                    {
                        if (!(p.AsValueString() == "" || p.AsValueString() == null))
                        {
                            listComboLevel.Add(p.AsValueString()); // Заполняю список результирующими значениями
                        }
                    }
                }
            }
            listComboLevel = listComboLevel.Distinct().ToList(); // Удаляю повторяющихся значений из списка
            // Проверяю список на пустату
            if (listComboLevel.Count != 0)
            {
                foreach (string item in listComboLevel)
                {
                    comboLevel.Items.Add(item); // Заполнение ComboValue полученнымы значениями из нашего списка
                }
                //comboLevel.SelectedIndex = 0; // По умолчанию отображается первый элемент (0) из списка в поле ввода 
            }
            else
            {
                MessageBox.Show("Ни одна комната не имеет значения для параметра уровень, пожалуйста заполните параметр", "Внимание!"); // Сообшаю об ошибке, если результирующий список пусть
            }
        }

        ////

        // Заполнение список значений
        private void comboValue_Enter(object sender, EventArgs e) // Когда сomboValue (Выпадающий список Значений) активируется мышю или клавиатурой
        {
            // Очистка списка значений
            comboValue.Items.Clear();
            listComboValue.Clear();
            // Получаю имена всех комнат по заданным параметрам
            FilteredElementCollector rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType();
            foreach (Room item in rooms)
            {
                if (/*item.LookupParameter("Уровень").StorageType == StorageType.ElementId &&*/ item.LookupParameter("Уровень").AsValueString() == comboLevel.Text)
                {
                    foreach (Parameter p in item.ParametersMap)
                    {
                        // Нахожу все типы отделки на данном уровне - вне контекста
                        if ((p.Definition.Name == "Отделка стен") && (p.AsValueString() != null /*&& p.AsValueString().Trim() != ""*/))
                        {
                            listDecorType.Add(p.AsValueString()); // Если выбрать в параметрах "Этаж" то listDecorType = null
                        }
                        // Основной контекст
                        if (p.Definition.Name == comboParameters.Text)
                        {
                            if (!(p.AsValueString() == "" || p.AsValueString() == null))
                            {
                                listComboValue.Add(p.AsValueString()); // Заполняю список результирующими значениями
                                break;
                            }
                        }
                    }
                }
            }
            listComboValue = listComboValue.Distinct().ToList(); // Удаляю повторяющихся значений из списка
            listDecorType = listDecorType.Distinct().ToList(); // Удаляю повторяющихся типов отделок и получу наименование типов

            // Проверяю список на пустоту
            if (listComboValue.Count != 0 && comboValue.Items.Count == 0)
            {
                foreach (string item in listComboValue)
                {
                    comboValue.Items.Add(item); // Заполнение ComboValue полученнымы значениями из нашего списка
                }
            }
            else if (listComboValue.Count == 0 && comboValue.Items.Count == 0) // Если результирующий список пуст
            {
                MessageBox.Show("У данного параметра не обноружено значения, либо поля не заполнены", "Внимание!"); // Сообшаю об ошибке, если результирующий список пусть
                comboValue.Text = "";
            }
        }

        ////

        // Вывод Основного результата
        private void buttonOK_Click(object sender, EventArgs e) // Когда кликнеться кнопка "OK"
        {
            // Получаю имена всех комнат по заданным параметрам
            FilteredElementCollector rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType();
            List<string> listRoomsSelectedName = new List<string>(); // Список для хранение и отображения результатов
            //List<Room> listRoomsSelectedEl = new List<Room>(); // Список для хранение и отображения результатов
            int comboValueCheck = 0; // Определитель (для проверок) 
            foreach (Room room in rooms)
            {
                if (room != null && room.LookupParameter("Уровень").StorageType == StorageType.ElementId && room.LookupParameter("Уровень").AsValueString() == comboLevel.Text)
                {
                    bool exitLoop = false; // Флаг для выхода из цикла в нужный момент, и предотвращение ненужных итераций
                    foreach (Parameter parameter in room.ParametersMap)
                    {
                        switch (comboCondition.Text)
                        {
                            case "равно":
                                {
                                    if (parameter.Definition.Name == comboParameters.Text && parameter.AsValueString() == comboValue.Text.TrimEnd()) // Если имя параметра равно "Параметр" и значение параметра равно "Значение"
                                    {
                                        listRoomsSelectedName.Add(room.Name); // Заполняю список, результирующими значениями
                                        listOfRooms.Add(room);
                                        exitLoop = true; // Нужный момент  
                                    }
                                    break;
                                }
                            case "не равно":
                                {
                                    if (parameter.Definition.Name == comboParameters.Text && parameter.AsValueString() != comboValue.Text.TrimEnd()) // Если имя параметра равно "Параметр" и значение параметра не равно "Значение"
                                    {
                                        listRoomsSelectedName.Add(room.Name);
                                        listOfRooms.Add(room);
                                        exitLoop = true; // Нужный момент
                                    }
                                    break;
                                }
                            case "содержит":
                                {
                                    if (parameter.Definition.Name == comboParameters.Text && parameter.AsValueString() != null) // Если имя параметра равно "Параметр" и значение параметра не равно "Значение"
                                    {
                                        if (parameter.AsValueString().Contains(comboValue.Text.TrimEnd()))
                                        {
                                            listRoomsSelectedName.Add(room.Name);
                                            listOfRooms.Add(room);
                                            exitLoop = true; // Нужный момент
                                            comboValueCheck = 3; // Определитель что было выбрано условие "содержит"                                            
                                        }
                                    }
                                    break;
                                }
                        }
                        if (exitLoop) // Выход из цикла
                        {
                            break;
                        }
                    }
                }
            }
            // Вывод Основного результата 
            // Проверка если есть ручной ввод, или выбора варианта
            if (comboValueCheck != 3)
            {
                if (listComboValue.Count != 0) // Если список значение не пуст
                {
                    foreach (string item in listComboValue)
                    {
                        if (comboValue.Text != "" && comboValue.Text.TrimEnd() != item) // Если ввод/выбор не совпадает с значением из списка
                        {
                            comboValueCheck = 1;
                        }
                        if (comboValue.Text != "" && comboValue.Text.TrimEnd() == item) // Если ввод/выбор совпадает с значением из списка
                        {
                            comboValueCheck = 2;
                            break;
                        }
                    }
                }
                else if (comboValue.Text.TrimEnd() != "") // Если список значение пуст, но есть ввод
                {
                    comboValueCheck = 1;
                }
                if (comboValueCheck == 1)
                {
                    MessageBox.Show("Ввод неправильных данных в поле 'Значение', либо у данного параметра по умолчанию отсутствует значение", "Внимание!");
                }
                // Проверка если нету ручного ввода
                else if (comboValue.Items.Count == 0 || comboValue.Text.TrimEnd() == "") // Если список значение пуст, и нету ввода
                {
                    MessageBox.Show("Поле 'Значение' пусто, либо у данного параметра по умолчанию отсутствует значение", "Внимание!");
                }
                else if (comboLevel.Items.Count == 0 || comboLevel.Text == "") // Если не выбран элемент из списка уровней 
                {
                    MessageBox.Show("Поле 'Уровень' пусто", "Внимание!");
                }
                else if (listRoomsSelectedName.Count == 0) // Если не найдено элементов 
                {
                    MessageBox.Show("Не найдено элементов", "Внимание!");
                }
                else
                {
                    listRoomsSelectedName.Add(listRoomsSelectedName.Count.ToString());
                    MessageBox.Show(String.Join("\n ", listRoomsSelectedName), "Результаты селектора"); // Вывод результатов (равно/не равно)
                }
            }
            else
            {
                listRoomsSelectedName.Add(listRoomsSelectedName.Count.ToString());
                MessageBox.Show(String.Join("\n ", listRoomsSelectedName), "Результаты селектора"); // Вывод результатов (содержит)
            }

            MessageBox.Show(String.Join("\n ", listDecorType), "Типы отделок"); // Вывод типов отделок (их наименование), listDecorType - список с наименованием типов отделок

            ////
            // Отсечение по типу отделки
            ////

            listOfRooms = listOfRooms.OrderBy(r => r.LookupParameter("Отделка стен").AsValueString()).ToList(); // Сортировка выбранных комнат по параметру "Отделка Стен", этот список будет передано для транзакции

            List<string> listOfNameAndDecoration = listOfRooms.Select(r => r.Name + " - " + r.LookupParameter("Отделка стен").AsValueString()).ToList();
            MessageBox.Show(String.Join("\n ", listOfNameAndDecoration), "listOfNameAndDecoration of listRoomsSelectedEl");

            #region
            //List<List<string>> listOfList = new List<List<string>>(); // Вложенный - материнский  список

            //if (listRoomsSelectedEl.Count != 0)
            //{
            //    // Добавляю дочерных списков (для каждого типа отделки) и Образцовый элемент для дочерных списков (помогает потом при заполнение значений)
            //    int counter = 0;
            //    for (int i = 0; i < listDecorType.Count; i++)
            //    {
            //        listOfList.Add(new List<string>()); // Добавляю дочерных списков 
            //        listOfList[i].Add(listDecorType[i]); // Добавляю Образцовый элемент в каждом листе
            //        listOfListRooms.Add(new List<Room>()); // Добавляю дочерных списков для  listOfListRoom                  
            //        counter++;
            //    }
            //    MessageBox.Show(listOfListRooms.Count.ToString(), "Attention");

            //    // Добавляю значения для всех дочерных списков
            //    bool check;
            //    for (int t = 0; t < listRoomsSelectedEl.Count; t++)
            //    {
            //        check = true;
            //        for (int i = 0; i < listOfList.Count; i++)
            //        {
            //            for (int j = 0; j < listOfList[i].Count; j++)
            //            {
            //                if (listRoomsSelectedEl[t].LookupParameter("Отделка стен").AsValueString() == listOfList[i][j])
            //                {
            //                    listOfList[i].Add(listRoomsSelectedEl[t].LookupParameter("Отделка стен").AsValueString()); // Добавление значений в дочерных листах (как значение парам."Отделка стен")
            //                    listOfListRooms[i].Add(listRoomsSelectedEl[t]); // Добавление значений в дочерных листах listOfListRoom (как Room)
            //                    check = false; // Для отмены ненужных итераций
            //                    break;
            //                }
            //            }
            //            if (!check)
            //            {
            //                break;
            //            }
            //        }
            //    }

            //    // Удаляю дочерные листы в котором кроме образцовых элементов нет ничего
            //    for (int i = listOfList.Count - 1; i > 0; i--)
            //    {
            //        if (listOfList[i].Count == 1 || listOfList[i].Count == 0)
            //        {
            //            listOfList.RemoveAt(i);
            //            listOfListRooms.RemoveAt(i);
            //        }
            //    }

            //    // Удаляю случайно появляющегося дочерного списка в соответсвии с уcловиями (этим исправил баг)
            //    if (listOfList.Count != 1 && listOfList.Count != listDecorType.Count)
            //    {
            //        listOfList.RemoveAt(0);
            //        listOfListRooms.RemoveAt(0);
            //    }

            //    // Удаляю образцовые элементы из дочерных листах
            //    for (int i = 0; i < listOfList.Count; i++)
            //    {
            //        if (listOfList[i].Count > 1)
            //        {
            //            listOfList[i].RemoveAt(0);
            //        }
            //    }

            //    // Вывод
            //    List<string> list_test_items_Rooms = new List<string>(); // Для вывода все элементы всех дочерных списков как одно целое
            //    List<string> list_test_items = new List<string>(); // Для вывода все элементы всех дочерных списков как одно целое
            //    foreach (var item in listOfList)
            //    {
            //        foreach (var item_2 in item)
            //        {
            //            list_test_items.Add(item_2);
            //        }
            //    }
            //    list_test_items.Add(list_test_items.Count.ToString());
            //    //MessageBox.Show(String.Join("\n", list_test_items), "Result list_test_items");
            //    //MessageBox.Show(String.Join("\n", listOfListRoom), "Result listOfListRoom");


            //    foreach (var item in listOfListRooms)
            //    {
            //        list_test_items_Rooms.Add("new List");
            //        foreach (var item_2 in item)
            //        {
            //            list_test_items_Rooms.Add(item_2.Name + "  :  " + item_2.LookupParameter("Отделка стен").AsValueString());
            //        }
            //    }
            //    list_test_items_Rooms.Add(list_test_items_Rooms.Count.ToString());

            //    MessageBox.Show(String.Join("\n", list_test_items_Rooms), "Result");
            // listOfRooms - конечный вложенный лист с результатами, cо группированными комнатамы по типу отделки (дочерные списки, каждый с его элементами (типы отделок)).
            // В дальнейщем работаем именно с ним
            ////
            ////
            #endregion

            // Вызываю Обработчик внешнего события (IExternalEventHandler), для проведение транзакции
            ExEvent.Raise();
            this.Close();
        }

        ////  

        private void comboParameters_SelectionChangeCommitted(object sender, EventArgs e) // Когда выбираеться значение из comboParameters (Выпадающий список Параметров)
        {
            // Очистка списка значений
            comboValue.Text = "";
            comboValue.Items.Clear();
        }

        ////

        private void comboLevel_SelectionChangeCommitted(object sender, EventArgs e) // Когда выбираеться значение из comboLevel (Выпадающий список Уровеней)    
        {
            // Очистка списка значений
            comboValue.Text = "";
            comboValue.Items.Clear();
        }

        ////

        private void buttonExit_Click(object sender, EventArgs e) // Когда кликнеться кнопка "Exit"
        {
            this.Close();
        }

    }

    ////
    ////

    public class ExternalEventClass : IExternalEventHandler
    {
        UIDocument uidoc;
        Document doc;
        //List<List<Room>> listOfListRoom_Got;
        List<Room> listOfRooms_Got;
        public ExternalEventClass(ExternalCommandData commandData, List<Room> listOfRooms)
        {
            uidoc = commandData.Application.ActiveUIDocument;
            doc = uidoc.Document;
            //listOfListRoom_Got = listOfListRooms;
            listOfRooms_Got = listOfRooms;
        }

        public void Execute(UIApplication app)
        {
            #region
            ViewPlan vp = doc.ActiveView as ViewPlan;
            if (vp == null)
            {
                TaskDialog.Show("t", "Данный вид не является ViewPlan");
            }
            else
            {
                #region Для тестов генерации отделки
                //// Для выборки по мыши тип отделки
                //TaskDialog.Show("t", "Выберите тип стены");
                //Reference refer = uidoc.Selection.PickObject(ObjectType.Element);

                // Для выборки по мыши одно помещение
                //TaskDialog.Show("t", "Выберите комнату");
                //Reference refer_Room = uidoc.Selection.PickObject(ObjectType.Element);
                //Room room = doc.GetElement(refer_Room) as Room;
                //

                // Для генерация отделки во всех помещениях данного уровня (без мыши)
                //FilteredElementCollector rooms_test = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType();
                //List<Room> rooms = new List<Room>();
                //foreach (Element item in rooms_test)
                //{
                //    rooms.Add(item as Room);
                //}
                //
                #endregion

                WallType wallType = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsElementType().
                    Where(w => w.Name.Equals("ADSK_Отделка_Условная_20")).FirstOrDefault() as WallType;

                ElementId wallType_Id = wallType.Id;
                double wallType_width = wallType.Width;

                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
                double DecorWallHeight = 0;

                // Здесь будут Case(ы) для разных отделок
                foreach (Room room in listOfRooms_Got)
                {
                    List<BoundarySegment> boundarySegments_list_Final = new List<BoundarySegment>();

                    // Фильтрация списка
                    foreach (List<BoundarySegment> boundarySegments_list in room.GetBoundarySegments(opt).ToList())
                    {
                        foreach (BoundarySegment item in boundarySegments_list)
                        {
                            if (doc.GetElement(item.ElementId) != null)
                            {
                                boundarySegments_list_Final.Add(item);
                            }
                        }
                    }

                    // Создание Отделки для стен
                    using (Transaction tx = new Transaction(doc, "Test"))
                    {
                        tx.Start("Transaction Start");
                        for (int i = 0; i < boundarySegments_list_Final.Count; i++)
                        {
                            if (doc.GetElement(boundarySegments_list_Final[i].ElementId).Category.Name == "Стены")
                            {
                                // Создаю Отделку для основных стен
                                Curve curve = boundarySegments_list_Final[i].GetCurve();
                                Curve curve_offset = curve.CreateOffset(wallType_width * (-1) / 2, new XYZ(0, 0, 1));
                                Wall wall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall;
                                ElementId levelId = wall.LevelId;
                                DecorWallHeight = mmToFeet(Convert.ToDouble(wall.LookupParameter("Неприсоединенная высота").AsValueString()));

                                Wall.Create(doc,
                                   curve_offset,
                                   wallType_Id,
                                   levelId,
                                   DecorWallHeight,
                                   0,
                                   false,
                                   false);


                                // Проверка торцов (Создаю отделку для торцевых частей)
                                Wall myWall;
                                Wall firstWall;
                                Wall secondWall;
                                Curve curve_2_offset;
                                if (i != 0 && (boundarySegments_list_Final[i].GetCurve().GetEndPoint(0).X != boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1).X ||
                                    boundarySegments_list_Final[i].GetCurve().GetEndPoint(0).Y != boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1).Y))
                                {
                                    Line curve_2;
                                    try
                                    {
                                        curve_2 = Line.CreateBound(boundarySegments_list_Final[i].GetCurve().GetEndPoint(0), boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1));
                                    }
                                    catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException ex)
                                    {
                                        goto Found;
                                    }

                                    firstWall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall; // Начальная точка курва
                                    secondWall = doc.GetElement(boundarySegments_list_Final[i - 1].ElementId) as Wall; // Конечная точка курва

                                    // Если отделка вокруг одной стены (с разных сторон стены в одной помещении)
                                    if ((firstWall != null && secondWall != null) && (firstWall.Id == secondWall.Id))
                                    {
                                        curve_2_offset = curve_2.CreateOffset(wallType_width / 2, new XYZ(0, 0, 1));
                                        DecorWallHeight = mmToFeet(Convert.ToDouble((doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall).
                                            LookupParameter("Неприсоединенная высота").AsValueString()));

                                        myWall = Wall.Create(doc,
                                           curve_2_offset,
                                           wallType_Id,
                                           levelId,
                                           DecorWallHeight, // < 7 работает но больше не будет стенька
                                           0,
                                           false,
                                           false);


                                        //// Соединяю торцовую отделку
                                        WallUtils.AllowWallJoinAtEnd(myWall, 0);
                                        WallUtils.AllowWallJoinAtEnd(myWall, 1);
                                    }
                                    else
                                    {
                                        curve_2_offset = curve_2.CreateOffset(wallType_width / 2, new XYZ(0, 0, 1));
                                        DecorWallHeight = mmToFeet(Convert.ToDouble((doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall).
                                            LookupParameter("Неприсоединенная высота").AsValueString()));

                                        myWall = Wall.Create(doc,
                                           curve_2_offset,
                                           wallType_Id,
                                           levelId,
                                           DecorWallHeight,
                                           0,
                                           false,
                                           false);

                                        // Отсоединяю торцовую отделку, от двух сторон (потом соединю с одной)
                                        WallUtils.DisallowWallJoinAtEnd(myWall, 0);
                                        WallUtils.DisallowWallJoinAtEnd(myWall, 1);


                                        // Соединяю торцовую отделку
                                        WallConnecting(firstWall, secondWall, myWall);
                                    }
                                }

                                // Если это последняя итерация (стена - торец)
                                if (i == boundarySegments_list_Final.Count - 1 && (boundarySegments_list_Final[i].GetCurve().GetEndPoint(1).X != boundarySegments_list_Final[0].GetCurve().GetEndPoint(0).X
                                    || boundarySegments_list_Final[i].GetCurve().GetEndPoint(1).Y != boundarySegments_list_Final[0].GetCurve().GetEndPoint(0).Y))
                                {
                                    Line curve_3;
                                    Curve curve_3_offset;
                                    try
                                    {
                                        curve_3 = Line.CreateBound(boundarySegments_list_Final[i].GetCurve().GetEndPoint(1), // Точ в точ наооборот к прежнему случай
                                                boundarySegments_list_Final[0].GetCurve().GetEndPoint(0));
                                    }
                                    catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException ex)
                                    {
                                        goto Found;
                                    }

                                    firstWall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall; // Начальная точка курва
                                    secondWall = doc.GetElement(boundarySegments_list_Final[0].ElementId) as Wall; // Конечная точка курва

                                    // Если отделка вокруг одной стены (с разных сторон стены в одной помещении)
                                    if ((firstWall != null && secondWall != null) && (firstWall.Id == secondWall.Id))
                                    {
                                        curve_3_offset = curve_3.CreateOffset(wallType_width * 10, new XYZ(0, 0, 1));
                                        DecorWallHeight = mmToFeet(Convert.ToDouble((doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall).
                                        LookupParameter("Неприсоединенная высота").AsValueString()));

                                        myWall = Wall.Create(doc,
                                           curve_3_offset,
                                           wallType_Id,
                                           levelId,
                                           DecorWallHeight,
                                           0,
                                           false,
                                           false);



                                        // Соединяю торцовую отделку
                                        WallUtils.AllowWallJoinAtEnd(myWall, 0);
                                        WallUtils.AllowWallJoinAtEnd(myWall, 1);
                                    }
                                    else
                                    {
                                        curve_3_offset = curve_3.CreateOffset(wallType_width / (-2), new XYZ(0, 0, 1));
                                        DecorWallHeight = mmToFeet(Convert.ToDouble((doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall).
                                            LookupParameter("Неприсоединенная высота").AsValueString()));

                                        myWall = Wall.Create(doc,
                                           curve_3_offset,
                                           wallType_Id,
                                           levelId,
                                           DecorWallHeight,
                                           0,
                                           false,
                                           false);

                                        // Отсоединяю торцовую отделку, от двух сторон (потом соединю с одной)
                                        WallUtils.DisallowWallJoinAtEnd(myWall, 0);
                                        WallUtils.DisallowWallJoinAtEnd(myWall, 1);


                                        // Соединяю торцовую отделку
                                        WallConnecting(firstWall, secondWall, myWall);
                                    }
                                }
                            Found:
                                int uselessNumber = 0;
                            }
                        }
                        tx.Commit();
                    }
                }
            }
            #endregion
            return;
        }


        //
        public string GetName()
        {
            return "External Event Example";
        }
        //
        public double mmToFeet(double mm)
        {
            double mmToFeet = mm / 304.8;
            return mmToFeet;
        }

        public double feetToMm(double feet)
        {
            double feetToMm = feet * 304.8;
            return feetToMm;
        }

        public void WallConnecting(Wall firstWall, Wall secondWall, Wall myWall)
        {
            if (firstWall != null && secondWall != null)
            {
                if (firstWall.Width < secondWall.Width) // Если последняя стена меньше чем первая
                {
                    WallUtils.AllowWallJoinAtEnd(myWall, 1); // Соединить начало торцевой отделки со стороны большой стены
                }
                else // Если первая стена меньше чем последняя
                {
                    WallUtils.AllowWallJoinAtEnd(myWall, 0); // Соединить конец торцевой отделки со стороны большой стены
                }
            }
        }
    }

}
