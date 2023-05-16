﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DummyDB.Core;
namespace HardLab5
{
    class ViewModelEditTable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public ObservableCollection<ViewModelEditTable> Items { get; } = new ObservableCollection<ViewModelEditTable>();
        private ObservableCollection<string> _names;

        public ObservableCollection<string> ListOfColumns
        {
            get { return _names; }
            set
            {
                _names = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.Controls.DataGrid DataGrid { get; set; }


        public TableScheme selectedScheme;
        public Table selectedTable;
        private DataTable _dataNewTable;
        public DataTable DataNewTable
        {
            get { return _dataNewTable; }
            set
            {
                _dataNewTable = value;
                OnPropertyChanged();
            }
        }

        private string _tableName;
        public string TableName
        {
            get { return _tableName; }
            set
            {
                _tableName = value;
                OnPropertyChanged();
            }
        }
        private string _message2;
        public string Message2
        {
            get { return _message2; }
            set
            {
                _message2 = value;
                OnPropertyChanged();
            }
        }

        private string _newColumnName;
        public string NewColumnName
        {
            get { return _newColumnName; }
            set
            {
                _newColumnName = value;
                OnPropertyChanged();
            }
        }

        private string _selectedColumn;
        public string SelectedColumn
        {
            get { return _selectedColumn; }
            set
            {
                _selectedColumn = value;
                OnPropertyChanged();
            }
        }

        private string _selectedColumnDelete;
        public string SelectedColumnDelete
        {
            get { return _selectedColumnDelete; }
            set
            {
                _selectedColumnDelete = value;
                OnPropertyChanged();
            }
        }

        private string _columnName;
        public string ColumnName
        {
            get { return _columnName; }
            set
            {
                //if (value == _text) return;
                _columnName = value;
                OnPropertyChanged();
            }
        }
        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        private bool _primary;
        public bool Primary
        {
            get { return _primary; }
            set
            {
                _primary = value;
                OnPropertyChanged();
            }
        }

        //public ViewModelEditTable()
        //{
        //    DataNewTable = MainViewModel.copyDataTable;
        //    TableName = MainViewModel.tableName;
        //    ObservableCollection<string> names = new ObservableCollection<string>();
        //    foreach (var column in DataNewTable.Columns)
        //    {
        //        names.Add(column.ToString());
        //    }
        //    ListOfColumns = names;
        //    ListOfColumns.Add("нет выбора");
        //}

        public ICommand AddRow => new DelegateCommand(param =>
        {
            DataRow newRow = DataNewTable.NewRow();
            DataNewTable.Rows.Add(newRow);
        });

        public ICommand CreateColumn => new DelegateCommand(param =>
        {
            Items.Add(new ViewModelEditTable());

        });

        public ICommand RemoveColumn => new DelegateCommand(param =>
        {
            int count = 0;
            foreach(var item in Items)
            {
                count += 1;
                if (count == Items.Count)
                {
                    Items.Remove(item);
                    break;
                }
            }
        });

        public ICommand DeleteRow => new DelegateCommand(param =>
        {
            if(DataGrid.SelectedItem == null)
            {
                MessageBox.Show("Строка для удаления не была выделена");
                return;
            }
            for(int i = 0; i < DataGrid.Columns.Count; i++)
            {
                if (DataGrid.Items[i] == DataGrid.SelectedItem)
                {
                    DialogResult dialogResult = MessageBox.Show($"Вы уверены, что хотите безвозвратно удалить {i+1} строку?", "Подтверждение действий", MessageBoxButtons.YesNo); ;
                    if (dialogResult == DialogResult.Yes)
                    {
                        DeleteTableRow(i);
                        UpdateTable();
                        return;
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        return;
                    }
                }
            }
        });

        public ICommand EditTable => new DelegateCommand(param =>
        {
            if (GetEx())
            {
                return;
            }
            if (MainViewModel.tableName != TableName && TableName != null && TableName != "")
            {
                CreateNewFiles();
            }
            if(AddNewColumn())
            {
                System.Windows.MessageBox.Show("Данные не заполнены до конца");
                return;
            }
            if (SelectedColumnDelete != null && SelectedColumnDelete != "нет выбора")
            {
                DialogResult dialogResult = MessageBox.Show("Вы уверены, что хотите безвозвратно удалить столбец?", "Подтверждение действий", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DeleteColumn();
                }
                else if (dialogResult == DialogResult.No)
                {
                    return;
                }
                
            }
            string jsonNewScheme = JsonSerializer.Serialize(selectedScheme);
            File.WriteAllText(MainViewModel.folderPath + $"\\{selectedScheme.Name}.json", jsonNewScheme);
            
            UpdateTable();
        });


        public ICommand EditRows => new DelegateCommand(param =>
        {
            if (EditTableData())
            {
                return;
            }
            RewriteCSV();
            //UpdateTable();
        });

        public bool GetEx()
        {
            if (SelectedColumn != null && SelectedColumn != "" && NewColumnName != null && NewColumnName != "" && SelectedColumn != "нет выбора")
            {
                if (CheckEqualsNames("EditColumnName"))
                {
                    System.Windows.MessageBox.Show("Попытка переименования столбца в уже существующее имя");
                    return true;
                }
            }
            if (Items != null)
            {
                if (CheckEqualsNames("CreateNewColumn"))
                {
                    System.Windows.MessageBox.Show("Попытка добавления столбца с уже существующим именем");
                    return true;
                }
            }
            return false;
        }

        public void CreateNewFiles()
        {
            File.Move(MainViewModel.folderPath + $"\\{selectedScheme.Name}.json", MainViewModel.folderPath + $"\\{TableName}.json");
            File.Move(MainViewModel.folderPath + $"\\{selectedScheme.Name}.csv", MainViewModel.folderPath + $"\\{TableName}.csv");
            selectedScheme.Name = TableName;
        }

        public void RemoveColumnName()
        {
            foreach (Column column in selectedScheme.Columns)
            {
                if (column.Name == SelectedColumn)
                {
                    column.Name = NewColumnName;
                }
            }
        }

        public bool AddNewColumn()
        {
            if(Items != null)
            {
                //int countOfColumn = 0;
                //foreach (Column column in selectedScheme.Columns)
                //{
                //    countOfColumn += 1;
                //}
                foreach (var item in Items)
                {
                    Column column = new Column();
                    if (item.ColumnName == null || item.ColumnName == "" || item.Type == null)
                    {
                        return true;
                    }
                    column.Name = item.ColumnName;
                    column.Type = item.Type;
                    column.IsPrimary = item.Primary;
                    selectedScheme.Columns.Add(column);
                    ListOfColumns.Add(column.Name);
                   // countOfColumn += 1;
                    AddColumnInTable(column);
                }
            }
            return false;
        }
        public bool CheckEqualsNames(string location)
        {
            if(location == "EditColumnName")
            {
                foreach (Column column in selectedScheme.Columns)
                {
                    if (column.Name == NewColumnName)
                    {
                        return true;
                    }
                }
                if (SelectedColumn != null && SelectedColumn != "" && NewColumnName != null && NewColumnName != "" && SelectedColumn != "нет выбора")
                {
                    RemoveColumnName();
                }
            }
            else if(location == "CreateNewColumn")
            {
                foreach (var item in Items)
                {
                    foreach (Column column in selectedScheme.Columns)
                    {
                        if ((column.Name == item.ColumnName || item.ColumnName == NewColumnName) && NewColumnName != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void AddColumnInTable(Column column)
        {
            foreach (var row in selectedTable.Rows)
            {
                switch (column.Type)
                {
                    case "uint":
                        {
                            row.Data.Add(column, 0);
                            break;
                        }
                    case "int":
                        {
                            row.Data.Add(column, 0);
                            break;
                        }
                    case "float":
                        {
                            row.Data.Add(column, 0);
                            break;
                        }
                    case "double":
                        {
                            row.Data.Add(column, 0);
                            break;
                        }
                    case "datetime":
                        {
                            row.Data.Add(column, DateTime.MinValue);
                            break;
                        }
                    case "string":
                        {
                            row.Data.Add(column, "");
                            break;
                        }
                }
            }
            RewriteCSV();
        }
        public void DeleteColumn()
        {
            foreach (Column column in selectedScheme.Columns)
            {
                if (column.Name == SelectedColumnDelete)
                {
                    selectedScheme.Columns.Remove(column);
                    ListOfColumns.Remove(column.Name);
                    foreach (var row in selectedTable.Rows)
                    {
                        row.Data.Remove(column);
                    }
                    break;
                }
            }
            RewriteCSV();
        }

        public void RewriteCSV()
        {
            string pathTable = MainViewModel.folderPath + $"\\{selectedScheme.Name}.csv";
            int count = 1;
            StringBuilder newFile = new StringBuilder();
            foreach (var row in selectedTable.Rows)
            {
                foreach (Column column in row.Data.Keys)
                {
                    if (count == selectedScheme.Columns.Count)
                    {
                        newFile.Append($"{row.Data[column]}" + "\n");
                        break;
                    }
                    newFile.Append($"{row.Data[column]}" + ";");
                    count += 1;
                }
                count = 1;
            }
            File.WriteAllText(pathTable, newFile.ToString());
        }

        public void DeleteTableRow(int rowNumber)
        {
            int count = 0;
            foreach(Row row in selectedTable.Rows)
            {
                if(count == rowNumber)
                {
                    selectedTable.Rows.Remove(row);
                    break;
                }
                count += 1;
            }
            RewriteCSV();
        }

        public void UpdateTable()
        {
            DataNewTable.Clear();
            DataTable dataTable = new DataTable();
            foreach (Column column in selectedScheme.Columns)
            {
                dataTable.Columns.Add(column.Name);
            }

            for (int i = 0; i < selectedTable.Rows.Count; i++)
            {
                DataRow newRow = dataTable.NewRow();
                foreach (var rows in selectedTable.Rows[i].Data)
                {
                    newRow[rows.Key.Name] = rows.Value;
                }
                dataTable.Rows.Add(newRow);
            }
            DataNewTable = dataTable;
            ClearData();
        }

        public void ClearData()
        {
            Items.Clear();
            NewColumnName = null;
            SelectedColumn = null;

            ListOfColumns.Clear();
            ObservableCollection<string> names = new ObservableCollection<string>();
            foreach (var column in DataNewTable.Columns)
            {
                names.Add(column.ToString());
            }
            ListOfColumns = names;
            ListOfColumns.Add("нет выбора");
        }
        public bool EditTableData()
        {
            for (int i = 0; i < selectedTable.Rows.Count; i++)
            {
                for (int j = 0; j < selectedScheme.Columns.Count; j++)
                {
                    //string data = DataNewTable.Rows[i][selectedScheme.Columns[j].Name].ToString();
                    if (selectedTable.Rows[i].Data[selectedScheme.Columns[j]].ToString() == DataNewTable.Rows[i][selectedScheme.Columns[j].Name].ToString())
                    {
                        continue;
                    }
                    object data = CheckCorrectData(i, j);
                    if(data == null) 
                    {
                        DataNewTable.Rows[i][selectedScheme.Columns[j].Name] = selectedTable.Rows[i].Data[selectedScheme.Columns[j]];
                        return true;
                    }
                    selectedTable.Rows[i].Data[selectedScheme.Columns[j]] = data;
                }
            }

            if(DataNewTable.Rows.Count > selectedTable.Rows.Count)
            {
                for (int i = selectedTable.Rows.Count; i < DataNewTable.Rows.Count; i++)
                {
                    Row row = new Row();
                    for (int j = 0; j < selectedScheme.Columns.Count; j++)
                    {
                        object data = CheckCorrectData(i, j);
                        if(data == null)
                        {
                            return true;
                        }
                        row.Data[selectedScheme.Columns[j]] = data;
                        DataNewTable.Rows[i][selectedScheme.Columns[j].Name] = data;
                    }
                    selectedTable.Rows.Add(row);
                }
             }
            return false;
        }

        public object CheckCorrectData(int numOfRow, int numOfColumn)
        {
            string data = DataNewTable.Rows[numOfRow][selectedScheme.Columns[numOfColumn].Name].ToString();
            switch (selectedScheme.Columns[numOfColumn].Type)
            {
                case "uint":
                    {
                        if (uint.TryParse(data, out uint number))
                        {
                            return number;
                        }
                        else if (ShowMessage(numOfRow + 1, numOfColumn + 1))
                        {
                            return 0;
                        }
                        return null;
                    }
                case "int":
                    {
                        if (int.TryParse(data, out int number))
                        {
                           return number;
                        }
                        else if (ShowMessage(numOfRow + 1, numOfColumn + 1))
                        {
                            return 0;
                        }
                        return null;
                    }
                case "float":
                    {
                        if (float.TryParse(data, out float number))
                        {
                            return number;
                        }
                        else if (ShowMessage(numOfRow + 1, numOfColumn + 1))
                        {
                            return 0;
                        }
                        return null;
                    }
                case "double":
                    {
                        if (double.TryParse(data, out double number))
                        {
                            return number;
                        }
                        else if (ShowMessage(numOfRow + 1, numOfColumn + 1))
                        {
                            return 0;
                        }
                        return null;
                    }
                case "datetime":
                    {
                        if (DateTime.TryParse(data, out DateTime number))
                        {
                            return number;
                        }
                        else if (ShowMessage(numOfRow + 1, numOfColumn + 1))
                        {
                            return DateTime.MinValue;
                        }
                        return null;
                    }
                case "string":
                    {
                        return data;
                    }
            }
            return null;
        }

       public bool ShowMessage(int numOfRow, int numOfColumn)
        {
            DialogResult dialogResult = MessageBox.Show($"В строке {numOfRow} столбце {numOfColumn} введены некорректные данные или найдены пустые ячейки, они будут заполнены значениями по умолчанию", "Подтверждение действий", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                return true;
            }
            else if(dialogResult == DialogResult.No)
            {
                return false;
            }
            return false;
        }
    }
}
