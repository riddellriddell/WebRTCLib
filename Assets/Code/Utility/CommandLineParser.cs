using System;
using System.Collections.Generic;
using System.Reflection;

public static class CommandLineParser
{
#if UNITY_EDITOR
    private static string[] s_strEditorCommandLineArgs;
#endif

    public static T Parse<T>() where T : class
    {
#if UNITY_EDITOR
        string[] strArgs = s_strEditorCommandLineArgs;
#else
        string[] strArgs = System.Environment.GetCommandLineArgs();
#endif
        PropertyInfo[] priProperties = typeof(T).GetProperties();
        PropertyInfo targetProperty = null;

        T output = default(T);

        for (int i = 1; i < strArgs.Length; i++)
        {
            if (strArgs[i].Length < 2)
            {
                continue;
            }

            //check if it is a setter
            if (strArgs[i][0] == '-')
            {
                string strArgumentName = strArgs[i].Substring(1);

                //try and find matching argument 
                for (int j = 0; j < priProperties.Length; j++)
                {
                    if (strArgumentName == priProperties[i].Name)
                    {
                        targetProperty = priProperties[i];

                        if (priProperties[i].PropertyType == typeof(bool))
                        {
                            priProperties[i].SetValue(output, true);
                        }
                    }
                }
            }
            else if (targetProperty != null)
            {
                if (priProperties[i].PropertyType == typeof(int))
                {
                    if (int.TryParse(strArgs[i], out int iValue))
                    {
                        priProperties[i].SetValue(output, iValue);
                    }
                }

                if (priProperties[i].PropertyType == typeof(List<int>))
                {
                    if (int.TryParse(strArgs[i], out int iValue))
                    {
                        List<int>  iList = priProperties[i].GetValue(output) as List<int>;
                        iList.Add(iValue);
                    }
                }

                if (priProperties[i].PropertyType == typeof(float))
                {
                    if(float.TryParse(strArgs[i], out float fValue))
                    {
                        priProperties[i].SetValue(output, fValue);
                    }
                }

                if (priProperties[i].PropertyType == typeof(List<float>))
                {
                    if (float.TryParse(strArgs[i], out float fValue))
                    {
                        List<float> fList = priProperties[i].GetValue(output) as List<float>;
                        fList.Add(fValue);
                    }
                }

                if (priProperties[i].PropertyType == typeof(string))
                {
                    priProperties[i].SetValue(output,strArgs[i]);
                }

                if (priProperties[i].PropertyType == typeof(List<string>))
                {
                    List<string> strList = priProperties[i].GetValue(output) as List<string>;

                    strList.Add(strArgs[i]);
                }

                if (priProperties[i].PropertyType.IsEnum)
                {
                    string[] strEnumNames = priProperties[i].PropertyType.GetEnumNames();

                    for(int j = 0; j < strEnumNames.Length; j++)
                    {
                        if(strEnumNames[j] == strArgs[i])
                        {
                            priProperties[i].SetValue(output, Convert.ChangeType(j, priProperties[i].GetType()));
                        }
                    }
                }
            }
        }

        return output;
    }
}
