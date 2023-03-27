using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Globalization;
using JetBrains.Annotations;

namespace GameCollector.SQLiteUtils;

// Based on https://stackoverflow.com/a/57541054

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class SqlColNameAttribute : Attribute
{
    private string _name = "";
    public string Name { get => _name; set => _name = value; }

    public SqlColNameAttribute(string name)
    {
        _name = name;
    }
}

[PublicAPI]
public static class SQLiteHelpers
{
    public static List<T>? ToList<T>(this DataTable table) where T : class, new()
    {
        try
        {
            List<T> list = new();

            foreach (var row in table.AsEnumerable())
            {
                var obj = new T();

                foreach (var prop in obj.GetType().GetProperties())
                {
                    try
                    {
                        //Set the column name to be the name of the property
                        var ColumnName = prop.Name;

                        //Get a list of all of the attributes on the property
                        var attrs = prop.GetCustomAttributes(inherit: true);
                        foreach (var attr in attrs)
                        {
                            //Check if there is a custom property name
                            if (attr is SqlColNameAttribute colName)
                            {
                                //If the custom column name is specified overwrite property name
                                if (!colName.Name.IsNullOrWhiteSpace())
                                    ColumnName = colName.Name;
                            }
                        }

                        var propertyInfo = obj.GetType().GetProperty(prop.Name);

                        //GET THE COLUMN NAME OFF THE ATTRIBUTE OR THE NAME OF THE PROPERTY
                        propertyInfo?.SetValue(obj, Convert.ChangeType(row[ColumnName], propertyInfo.PropertyType, CultureInfo.InvariantCulture), index: null);
                    }
                    catch
                    {
                        continue;
                    }
                }

                list.Add(obj);
            }

            return list;
        }
        catch
        {
            return null;
        }
    }

    public static DataTable GetDataTable(string file, string query)
    {
        SQLiteConnection connection = new($"Data source={file}");
        try
        {
            DataTable data = new();
            connection.Open();
            using (SQLiteCommand command = new(query, connection))
            {
                data.Load(command.ExecuteReader());
            }
            return data;

        }
        catch (Exception)
        {
            return new();
        }
        finally
        {
            connection.Close();
        }
    }

    public static bool IsNullOrWhiteSpace(this string x)
    {
        return string.IsNullOrWhiteSpace(x);
    }
}
