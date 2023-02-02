#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

#endregion

namespace Metal_Rolling
{
    /*                                  Начать
                                     17 - задача   
    
    -                                   Задачи
        * 1. Заполнить аттрибутов "ADSK_Количества" и "ADSK_Масса" и "Наименование"   -> Выполнена 
        * 2. Управлять количество столбцов во время создания, путем преждевременное измерение их количества и поправки, т.е. добавление в случае < Matrix.Length(1) и удаление в случае > Matrix.Getlentgh(1) -> Выполнена
        * 3 MergeCells составляющих элементов ограждений по "ОГЛ" -> Выполнена
        * 4. Подправить размеры столбцов и строк под контента -> Выполнена
        * 5. Добавить в начале списке Строка заголовки с названием Столбцов и потом уже остальное, но это напоследок -> Выполнена
        * 6. Упорядочить по ADSK_Марка -> Выполнена 
        * 7. Исправить ситуацию с Active.ViewSchedule сделать независимым -> Выполнена
       
                                         01.12
        * 8. Обновление таблицы при повторном использование -> Выполнена
        * 9. Полоса поле "Наименование подправить согласно инструкции Рената на бумаге" -> Выполнена
        * 10. Статические размеры для столбцов таблицы -> Выполнена
        * 11. TableSeсtionData.SetCellStyle() - Оформление: название шрифта, размер шрифта, Alignment -> Выполнена
        * 12. TableSeсtionData.SetCellStyle() - жирные границы для каждой ограждений -> 10% (Создать вспомогательные строки и столбцы для изменения границ и потом их удалить)
        * 13. Подправить размер строк -> Выполнена
        * 14. Исправить Суммарник массы и Отображение Количество -> Выполнена
        
        * 15. Исправить поле наименование, улучшить -> Выполнена
        * 16. Поле наименование "Пруток" исправить вид -> Выполнена
         
        * -- 17. Перенос часть таблицы после 8-го образца (Еще раз уточнить номер образца)
        * 18. Уточнить момент с различными спецификациями, и необходимости ручного регулирование их контента (все что ниже основной заголовки) + что будет со спец-шрифтом, надо ли установить каждый раз?
          *- Если проект прошел все тесты :
        
        * Сделать для всех ссылающих параметров, переменные в начале, чтобы потом было легче их изменить
        * В конце посмотреть какие параметры/аргументы (даже кроме ревита) могут изменится, и хранить их в переменных, которые объявлени в начале
        * Code Refactoring
    
     -                        Добавленные фичи и изменение
         *                                28.11
           1 задача
           2 задача
         *                                29.11
           3 задача
           4 задача
           5 задача
           6 задача
         *                                01.12
           7 задача                                
         *                                02.12
           8 задача     
           9 задача
           10 задача
         *                                05.12
           11 задача
           13 задача
         *                                06.12
           14 задача
         *                                12.12
           15 задача
           16 задача
         *                                14.12
           
        */



    #region 
    [Transaction(TransactionMode.Manual)]
    public class Metal_Rolling_Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Fields
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            #endregion







            // Получаю все экземпляры ограждений в проекте
            #region
            List<FamilyInstance> fences_Instances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StairsRailing).WhereElementIsNotElementType()
                .Cast<FamilyInstance>().OrderBy(x => x.Symbol.LookupParameter("ADSK_Марка").AsValueString()).ToList();
            #endregion







            // Получаю размер строков массива
            #region
            List<FamilyInstance> te = fences_Instances.DistinctBy(fam => fam.Name).ToList(); // Получаю образцовые ограждений, с каждого типа (путем удаления похожих)
            int MatrixRowCount = 0;
            int instanceCounter = 0;
            foreach (FamilyInstance item in te)
            {
                List<FamilyInstance> Test = new List<FamilyInstance>();
                foreach (var item_2 in item.GetSubComponentIds())
                {
                    Test.Add(doc.GetElement(item_2) as FamilyInstance);
                }
                MatrixRowCount += Test.DistinctBy(fam => fam.LookupParameter("ADSK_Позиция ведомость элементов").AsValueString()).Count();
                instanceCounter++;
            }
            #endregion







            // Объявляю массив с определеннымы размерамы
            #region
            string[,] Matrix = new string[MatrixRowCount + 1, 8];
            string[] Arr_sech_prof_and_oboznach = new string[MatrixRowCount];
            string[] Arr_str_mat = new string[MatrixRowCount];
            #endregion







            // Создаю вложенный список, для обработки данных, чтобы потом поставить в массив и добавляю суммарники для "ADSK_Количества" и "ADSK_Масса" 
            #region 
            List<List<FamilyInstance>> ListofLists_fencesExamples = new List<List<FamilyInstance>>();
            List<List<int>> ListofLists_ADSK_Amount = new List<List<int>>();
            List<double> list_ADSK_Weight_Amount = new List<double>();
            double ADSK_Weight_Counter = 0;
            for (int i = 0; i < te.Count; i++)
            {
                ListofLists_fencesExamples.Add(new List<FamilyInstance>());
                ListofLists_ADSK_Amount.Add(new List<int>());
                List<FamilyInstance> famTest = new List<FamilyInstance>();
                foreach (var item_2 in te[i].GetSubComponentIds())
                {
                    FamilyInstance fam = doc.GetElement(item_2) as FamilyInstance;
                    famTest.Add(fam); // Все составляющие элементы одной ограждений
                    if (fam.Name != "RSm_БолтАнкерный_r22_v0.01")
                    {
                        ADSK_Weight_Counter += fam.LookupParameter("ADSK_Масса").AsDouble();
                    }
                }
                // Заполняю вложенный список  
                ListofLists_fencesExamples[i] = famTest.DistinctBy(elem => elem.LookupParameter("ADSK_Позиция ведомость элементов").AsValueString())
                    .OrderBy/*(x => x.LookupParameter("ADSK_Марка").AsValueString()).ThenBy*/(x => x.LookupParameter("ADSK_Позиция ведомость элементов").AsValueString()).ToList(); // DistinctBy and OrderBy
                // Получаю количество каждый составляющий элемент
                for (int w = 0; w < famTest.Count; w++)
                {
                    foreach (Parameter item in famTest[w].Parameters)
                    {
                        if (item.Definition.Name == "ADSK_Количество" && item.AsDouble() == 0)
                        {
                            famTest.RemoveAt(w);
                            break;
                        }
                    }
                }
                var t_str = famTest.Select(fam => fam.LookupParameter("ADSK_Позиция ведомость элементов").AsValueString()).ToList();
                t_str.Sort();
                ListofLists_ADSK_Amount[i] = t_str.GroupBy(x => x).Where(g => g.Count() > 0).Select(x => x.Count()).ToList();
                // Получаю сумма масс составляющих элементов
                list_ADSK_Weight_Amount.Add(ADSK_Weight_Counter);
                ADSK_Weight_Counter = 0;
            }
            #endregion







            // Собираю все в массив, чтобы потом поставить в заголовку (как таблица)
            #region         

            List<Material> Materials = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList();
            int listCounter = 0;
            string MirrorFor_Matrix_0_2 = "";
            for (int p = 0; p < ListofLists_fencesExamples.Count; p++)
            {
                #region
                if (p == 0) // Если это первое ограждение
                {
                    for (int i = 0; i < ListofLists_fencesExamples[p].Count; i++) // i - как строка массива 
                    {
                        if (ListofLists_fencesExamples[p][i].Name == "RSm_БолтАнкерный_r22_v0.01") // Если Анкерный болт
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            Matrix[i, 2] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Наименование").AsValueString();
                            Matrix[i, 3] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Наименование").AsValueString();
                            Matrix[i, 4] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Наименование").AsValueString();

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = "-";

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        else if (ListofLists_fencesExamples[p][i].Name == "RS_ПрокатАрматурный_ГОСТ34028-2016_r22_v0.01") // Если Пруток
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            // Наименование подготовка начало
                            string str_naim = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Наименование краткое").AsValueString();
                            string str_Sost_postavk = ListofLists_fencesExamples[p][i].LookupParameter("Состояние поставки").AsValueString();
                            string str_D_test = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Диаметр арматуры").AsValueString();
                            string str_Diametr = str_D_test.Substring(0, str_D_test.IndexOf(' '));
                            string str_Dlina = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Размер_Длина").AsValueString();
                            string str_Klass_armatura = ListofLists_fencesExamples[p][i].LookupParameter("Класс арматуры").AsValueString();
                            string str_Oboznach = ListofLists_fencesExamples[p][i].Symbol.LookupParameter("ADSK_Обозначение").AsValueString();
                            // Наименование подготовка конец

                            Matrix[i, 2] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 3] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 4] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Масса").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        else // Иначе
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            // Наименование подготовка начало
                            string str_naim_kratk = "";
                            string str_sech_prof = "";
                            string str_oboznach = "";
                            string str_mat = "";
                            string str_dlina = "";

                            str_naim_kratk = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Наименование краткое").AsValueString();
                            if (ListofLists_fencesExamples[p][i].Name == "RSh_Труба_ГОСТ32931-2015_r22_v0.01")
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][i].LookupParameter("Сечение профиля").AsValueString();
                            }
                            else
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][i].LookupParameter("Сечение полосы").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][i].LookupParameter("Точность прокатки").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][i].LookupParameter("Класс серповидности").AsValueString();
                            }
                            str_oboznach = ListofLists_fencesExamples[p][i].Symbol.LookupParameter("ADSK_Обозначение").AsValueString();
                            foreach (var item in Materials)
                            {
                                if (ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Материал").AsValueString() == item.Name)
                                {
                                    str_mat = item.LookupParameter("ADSK_Материал наименование").AsValueString() + "  " + item.LookupParameter("ADSK_Материал обозначение").AsValueString();
                                }
                            }
                            str_dlina = "L=" + ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Размер_Длина").AsValueString() + "мм";
                            string sech_prof_and_oboznach = str_sech_prof + "  " + str_oboznach;
                            MirrorFor_Matrix_0_2 = str_naim_kratk + sech_prof_and_oboznach + str_dlina;
                            // Наименование подготовка конец

                            Matrix[i, 2] = str_naim_kratk;
                            Matrix[i, 3] = sech_prof_and_oboznach + "\n" + str_mat;
                            Arr_sech_prof_and_oboznach[i] = sech_prof_and_oboznach;
                            Arr_str_mat[i] = str_mat;

                            Matrix[i, 4] = str_dlina;
                            // 
                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][i].LookupParameter("ADSK_Масса").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        // В будущем, если появляется новые составляющие элементы, то прости добавить новое условие с кодом в теле
                    }
                    listCounter += ListofLists_fencesExamples[p].Count;
                }

                #endregion
                #region
                else // Если это НЕ первое ограждение (в общем все точ в точ, как в первой условии)
                {
                    int someNumber = 0;
                    for (int i = listCounter; i < listCounter + ListofLists_fencesExamples[p].Count; i++) // i - как строка массива 
                    {
                        if (ListofLists_fencesExamples[p][someNumber].Name == "RSm_БолтАнкерный_r22_v0.01") // Если Анкерный болт
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            Matrix[i, 2] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Наименование").AsValueString();
                            Matrix[i, 3] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Наименование").AsValueString();
                            Matrix[i, 4] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Наименование").AsValueString();

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = "-";

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        else if (ListofLists_fencesExamples[p][someNumber].Name == "RS_ПрокатАрматурный_ГОСТ34028-2016_r22_v0.01") // Если Пруток
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            // Наименование подготовка начало
                            string str_naim = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Наименование краткое").AsValueString();
                            string str_Sost_postavk = ListofLists_fencesExamples[p][someNumber].LookupParameter("Состояние поставки").AsValueString();
                            string str_D_test = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Диаметр арматуры").AsValueString();
                            string str_Diametr = str_D_test.Substring(0, str_D_test.IndexOf(' '));
                            string str_Dlina = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Размер_Длина").AsValueString();
                            string str_Klass_armatura = ListofLists_fencesExamples[p][someNumber].LookupParameter("Класс арматуры").AsValueString();
                            string str_Oboznach = ListofLists_fencesExamples[p][someNumber].Symbol.LookupParameter("ADSK_Обозначение").AsValueString();
                            // Наименование подготовка конец
                            Matrix[i, 2] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 3] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 4] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Масса").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        else // Иначе
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_Марка").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Позиция ведомость элементов").AsValueString();

                            // Наименование подготовка начало
                            string str_naim_kratk = "";
                            string str_sech_prof = "";
                            string str_oboznach = "";
                            string str_mat = "";
                            string str_dlina = "";

                            str_naim_kratk = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Наименование краткое").AsValueString();
                            if (ListofLists_fencesExamples[p][someNumber].Name == "RSh_Труба_ГОСТ32931-2015_r22_v0.01")
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][someNumber].LookupParameter("Сечение профиля").AsValueString();
                            }
                            else
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][someNumber].LookupParameter("Сечение полосы").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][someNumber].LookupParameter("Точность прокатки").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][someNumber].LookupParameter("Класс серповидности").AsValueString();
                            }
                            str_oboznach = ListofLists_fencesExamples[p][someNumber].Symbol.LookupParameter("ADSK_Обозначение").AsValueString();
                            foreach (var item in Materials)
                            {
                                if (ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Материал").AsValueString() == item.Name)
                                {
                                    str_mat = item.LookupParameter("ADSK_Материал наименование").AsValueString() + " " + item.LookupParameter("ADSK_Материал обозначение").AsValueString();
                                }
                            }
                            str_dlina = "L=" + ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Размер_Длина").AsValueString() + "мм";

                            string sech_prof_and_oboznach = str_sech_prof + str_oboznach;
                            string whyteSpace = "";
                            string Space_naim_krat = whyteSpace.PadRight(str_naim_kratk.Length);
                            //// Наименование подготовка конец
                            Matrix[i, 2] = str_naim_kratk;
                            Matrix[i, 3] = sech_prof_and_oboznach + "\n" + str_mat;
                            Arr_sech_prof_and_oboznach[i] = sech_prof_and_oboznach;
                            Arr_str_mat[i] = str_mat;

                            Matrix[i, 4] = str_dlina;
                            //

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_Масса").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        // В будущем, если появляется новые составляющие элементы, то прости добавить новое условие с кодом в теле
                    }
                    listCounter += ListofLists_fencesExamples[p].Count;
                }
                #endregion 
            }
            Matrix = Array2DRowDoubler(Matrix);
            Arr_sech_prof_and_oboznach = ArrayRowDoubler(Arr_sech_prof_and_oboznach);
            Arr_str_mat = ArrayRowDoubler(Arr_str_mat);
            #endregion







            // Этап поставки в заголовке            
            #region
            ViewSchedule vs = doc.ActiveView as ViewSchedule; // Получаю авктивный вид как вид Спецификации/Ведомости
            if (null == vs)
            {
                TaskDialog.Show("t", "Данный вид не является спецификацией"); // Ошибка если спецификацией вид не спецификация
            }
            else
            {
                using (Transaction tx = new Transaction(doc))
                {
                    // 1 Letter Revit/VS = 0.01736 mm
                    // 1 mm = 1/304.8 feet
                    // 1 letter.width = letter * 0.01736 / 304.8
                    //double simple_letter_To_Revit_Letter = (3 * 1.736 / 304.8) / 2;


                    // Начинаю транзакцию
                    tx.Start("Transaction Name");
                    TableData td = vs.GetTableData(); // get viewschedule table data
                    TableSectionData tsd = td.GetSectionData(SectionType.Header); // get header section data
                    // Обновление таблицы при повторном использование                
                    if (tsd.NumberOfRows > 1)
                    {
                        // удаляем строки
                        for (int r = tsd.NumberOfRows - 1; r > 0; r--)
                        {
                            tsd.RemoveRow(r);
                        }
                    }
                    // MergeCells Объявление начато - Объединяю ячейек в нужных столбцах
                    TableMergedCell mergecell_Marka = new TableMergedCell();
                    mergecell_Marka.Left = 0;
                    mergecell_Marka.Right = 0;
                    mergecell_Marka.Top = 1;
                    mergecell_Marka.Bottom = 0;

                    TableMergedCell mergecell_Massa = new TableMergedCell();
                    mergecell_Massa.Left = 7;
                    mergecell_Massa.Right = 7;
                    mergecell_Massa.Top = 1;
                    mergecell_Massa.Bottom = 0;
                    // MergeCells Объявление завершено


                    // Заполняю ячеек
                    for (int i = 0; i < Matrix.GetLength(0); i++)
                    {
                        if (i == 0) // Проверяю если есть уже строка то удаляю ее и создаю новую
                        {
                            tsd.InsertRow(1);
                            tsd.RemoveRow(0);
                            // Регулировка число столбцов (начало)
                            if (tsd.NumberOfColumns < Matrix.GetLength(1))
                            {
                                for (int m = tsd.NumberOfColumns; m < Matrix.GetLength(1); m++)
                                {
                                    tsd.InsertColumn(m);
                                }
                            }
                            else
                            {
                                for (int m = Matrix.GetLength(1); m < tsd.NumberOfColumns; m++)
                                {
                                    tsd.RemoveColumn(m);
                                }
                            }
                            // Регулировка число столбцов (конец) 

                            // Подправление размер Столбцов таблицы - начато                        
                            for (int j = 0; j < Matrix.GetLength(1); j++)
                            {
                                if (j == 0 || j == 6 || j == 7)
                                {
                                    tsd.SetColumnWidth(j, 15 / 304.8 /*(MirrorFor_Matrix_0_2.Length + 15) * simple_letter_To_Revit_Letter*/);
                                }
                                else if (j == 1 || j == 5)
                                {
                                    tsd.SetColumnWidth(j, 10 / 304.8 /*(Matrix[0, j].Length + 5) * simple_letter_To_Revit_Letter*/);
                                }
                                else if (j == 2 || j == 4)
                                {
                                    tsd.SetColumnWidth(j, 14 / 304.8);
                                }
                                else if (j == 3)
                                {
                                    tsd.SetColumnWidth(j, 32 / 304.8);
                                }
                            }
                            // Подправление размер Столбцов таблицы - закончено (Подправление строк таблицы будет дальше)

                            // Устанавливаю название столбцов таблицы
                            tsd.SetCellText(i, 0, "Марка изделия");
                            tsd.SetCellText(i, 1, "Поз. дет.");
                            tsd.SetCellText(i, 2, "Наименование");
                            tsd.SetCellText(i, 3, "Наименование");
                            tsd.SetCellText(i, 4, "Наименование");
                            tsd.SetCellText(i, 5, "Кол.");
                            tsd.SetCellText(i, 6, "Масса 1 дет., кг");
                            tsd.SetCellText(i, 7, "Масса изделия, кг");

                            // Подправление размер строк таблицы - header
                            tsd.SetRowHeight(i, 15 / 304.8);
                        }
                        else // Если это не первая строка
                        {
                            tsd.InsertRow(i); // Добавляю новую строку

                            for (int j = 0; j < Matrix.GetLength(1); j++)
                            {
                                if (Matrix[i - 1, j] != null)
                                {
                                    tsd.SetCellText(i, j, Matrix[i - 1, j]);  // Если оставить все как есть то вместе первой строки массива пойдет втр строка, а в таблица после загаловки уже будет не перв в втр запись
                                }
                            }

                            // MergeCells логика начало
                            if (tsd.GetCellText(i, 0) != tsd.GetCellText(mergecell_Marka.Top, 0))
                            {
                                mergecell_Marka.Bottom = i - 1;
                                mergecell_Massa.Bottom = i - 1;
                                tsd.MergeCells(mergecell_Marka);
                                tsd.MergeCells(mergecell_Massa);
                                mergecell_Marka.Top = i;
                                mergecell_Massa.Top = i;
                            }
                            if (i == tsd.NumberOfRows - 1)
                            {
                                mergecell_Marka.Bottom = i;
                                mergecell_Massa.Bottom = i;
                                tsd.MergeCells(mergecell_Marka);
                                tsd.MergeCells(mergecell_Massa);
                            }
                            // MergeCells логика завершено

                            // Подправление размер строк таблицы - body
                            tsd.SetRowHeight(i, 8 / 304.8);
                        }
                        // Подправление размер строк таблицы

                    }

                    // Удаляю полностю пустые строки
                    string deleteCheck = "";
                    for (int i = 0; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 0; j < tsd.NumberOfColumns; j++)
                        {
                            deleteCheck += tsd.GetCellText(i, j).Trim();
                        }
                        if (deleteCheck.Trim() == "")
                        {
                            tsd.RemoveRow(i);
                        }
                        deleteCheck = "";
                    }
                    ////

                    // Форматирование ячейек

                    // Get the current style of the title cell
                    //TableCellStyle tableCellStyle = tsd.GetTableCellStyle(1, 1);
                    TableCellStyle tableCellStyle = new TableCellStyle();

                    // Get the override options of the style
                    //TableCellStyleOverrideOptions overrideOptions = tableCellStyle.GetCellStyleOverrideOptions();
                    TableCellStyleOverrideOptions overrideOptions = new TableCellStyleOverrideOptions();

                    // Font Size
                    overrideOptions.FontSize = true;

                    // Font Name
                    overrideOptions.Font = true;

                    // Horizontal Alignment
                    overrideOptions.HorizontalAlignment = true;

                    // Set the overrides options on the style
                    tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    // Change Font Name
                    tableCellStyle.FontName = "GOST Common";

                    // Устанавливаю размер шрифта для всех ячеек, заголовка 3.5 мм, содержимое 2.5 мм
                    for (int i = 0; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 0; j < tsd.NumberOfColumns; j++)
                        {
                            tableCellStyle.TextSize = 2.5 * 3.77938;
                            tsd.SetCellStyle(i, j, tableCellStyle);
                        }
                    }

                    // Horizontal Alignment                  
                    tableCellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Left;
                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        tsd.SetCellStyle(i, 2, tableCellStyle); // Для остальных две поля Наименование
                    }

                    //

                    // 15-я в основном задача начинается отсюда

                    // Поставляю значения для дробной части из объединенных параметров Sech_prof и  mat division 
                    for (int i = 1; i < tsd.NumberOfRows - 1; i++)
                    {
                        if (Arr_sech_prof_and_oboznach[i - 1] != null && Arr_str_mat[i - 1] != null)
                        {
                            if (i % 2 != 0)
                            {
                                tsd.SetCellText(i, 3, Arr_sech_prof_and_oboznach[i - 1]);
                            }
                            else
                            {
                                tsd.SetCellText(i, 3, Arr_str_mat[i - 1]);
                            }
                        }
                    }
                    ////



                    //// Жирные границы для каждой ограждений 
                    //GraphicsStyle GraphicsStyle_lineStyle = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().FirstOrDefault(e => e.Name.Equals("<Утолщенные линии>"));
                    //overrideOptions.BorderLineStyle = true;
                    //overrideOptions.BorderBottomLineStyle = true;
                    //overrideOptions.BorderTopLineStyle = true;
                    //overrideOptions.BorderLeftLineStyle = true;
                    //overrideOptions.BorderRightLineStyle = true;

                    //tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    //tableCellStyle.BorderTopLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderBottomLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderRightLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderLeftLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;

                    ////tableCellStyle.BorderTopLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderBottomLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderRightLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderLeftLineStyle = GraphicsStyle_lineStyle.Id;

                    //tsd.SetCellStyle(1, 2, tableCellStyle);
                    ////tsd.SetCellStyle(2, 3, tableCellStyle);
                    ////tsd.SetCellStyle(1, 4, tableCellStyle);
                    ////tsd.SetCellStyle(2, 4, tableCellStyle);

                    ////

                    
                    //// Стирание границ для поле наименований
                    overrideOptions.BorderLineStyle = true;
                    overrideOptions.BorderBottomLineStyle = false;
                    overrideOptions.BorderTopLineStyle = false;
                    overrideOptions.BorderLeftLineStyle = true;
                    overrideOptions.BorderRightLineStyle = false;

                    tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 3; j < 5; j++) // Так как BorderLineStyle работает только с Left корректируем удаляемую границу
                        {
                            tsd.SetCellStyle(i, j, tableCellStyle);
                        }
                        tsd.SetRowHeight(i, 4 / 304.8); // SetRowHeight for Doubled rows
                    }
                    
                    //// Merging doubled rows, and column Наименование среднее (у него еще 3 столбца)
                    TableMergedCell mergecell_test = new TableMergedCell();

                    // Merge Наименование Горизонтально, его три столбца
                    mergecell_test.Left = 2;
                    mergecell_test.Right = 4;
                    mergecell_test.Top = 0;
                    mergecell_test.Bottom = 0;
                    tsd.MergeCells(mergecell_test);

                    // Merge Остальных кроме 4-й столбец
                    for (int i = 1; i < tsd.NumberOfRows; i += 2)
                    {
                        for (int j = 1; j < 7; j++)
                        {
                            if (j != 3 || (j == 3 && (tsd.GetCellText(i, j).Contains("Анкерный") || tsd.GetCellText(i, j).Contains("Пруток"))))
                            {
                                mergecell_test.Left = j;
                                mergecell_test.Right = j;
                                mergecell_test.Top = i;
                                mergecell_test.Bottom = i + 1;
                                tsd.MergeCells(mergecell_test);
                            }
                        }
                        for (int j = 1; j < 7; j++)
                        {
                            if (tsd.GetCellText(i, j).Contains("Анкерный") || tsd.GetCellText(i, j).Contains("Пруток"))
                            {
                                for (int c = 2; c < 5; c++)
                                {
                                    mergecell_test.Left = 2;
                                    mergecell_test.Right = 4;
                                    mergecell_test.Top = i;
                                    mergecell_test.Bottom = i + 1;
                                    tsd.MergeCells(mergecell_test);
                                }
                                break;
                            }
                        }
                    }

                    // HorizontalAlignmentStyle.Center for "L="
                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        tableCellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Center;
                        tsd.SetCellStyle(i, 4, tableCellStyle); // Для остальных две поля Наименование
                    }
                    
                    // Заканчиваю транзакцию
                    tx.Commit();
                }
            }
            #endregion
            return Result.Succeeded;
        }

        static string[,] Array2DRowDoubler(string[,] Array2D)
        {
            string[,] RowDoubled_2DArray = new string[Array2D.GetLength(0) * 2, Array2D.GetLength(1)];
            int a = 0;
            for (int i = 0; i < Array2D.GetLength(0); i++)
            {
                for (int c = 0; c < 2; c++)
                {
                    for (int j = 0; j < Array2D.GetLength(1); j++)
                    {
                        RowDoubled_2DArray[a, j] = Array2D[i, j];
                    }
                    a++;
                }
            }
            return RowDoubled_2DArray;
        } // Method for Doubling Rows of 2D Array
        static string[] ArrayRowDoubler(string[] Array)
        {
            string[] RowDoubled_Array = new string[Array.Length * 2];
            int a = 0;
            for (int i = 0; i < Array.Length; i++)
            {
                for (int c = 0; c < 2; c++)
                {
                    RowDoubled_Array[a] = Array[i];
                    a++;
                }
            }
            return RowDoubled_Array;
        } // Method for Doubling Rows of Array
    }
    #endregion
}
