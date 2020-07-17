using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.IO;

public class ConfigReader
{
    static ConfigReader _instance = null;
    public static ConfigReader Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ConfigReader();
            return _instance;
        }
    }

    Dictionary<string, IDictionary> configDic;
    List<string> configNameList;

    bool isInit = false;

    /// <summary>
    /// 初始化时读取配置表
    /// </summary>
    public ConfigReader()
    {

    }

    #region 配置表字典初始化

    public void Init()
    {
        if (isInit)
            return;

        isInit = true;

        configDic = new Dictionary<string, IDictionary>();

        string path = Application.dataPath + "/Resources/Config/";
        configNameList = new List<string>();
        if (Directory.Exists(path))
        {
            DirectoryInfo direction = new DirectoryInfo(path);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo info in files)
            {
                if (info.Name.EndsWith(".meta"))
                {
                    continue;
                }
                configNameList.Add(Path.GetFileNameWithoutExtension(info.ToString()));
            }
        }

        if (configNameList.Count <= 0)
            return;

        Type configReaderType = this.GetType();
        foreach (string fileName in configNameList)
        {
            string name = fileName;
            Type type = Type.GetType(name);
            Type generic = typeof(Dictionary<,>);
            Type[] typeArr = { typeof(int), type };
            generic = generic.MakeGenericType(typeArr);
            IDictionary dic = Activator.CreateInstance(generic) as IDictionary;

            MethodInfo mi = configReaderType.GetMethod("InitConfigDic").MakeGenericMethod(type);
            dic=mi.Invoke(this, new object[] { fileName }) as IDictionary;

            configDic.Add(fileName, dic);
        }
    }

    /// <summary>
    /// 读取txt配置表
    /// 反射对配置表Model内的对象自动赋值
    /// 要求：
    /// 1.配置表名字必须为***Config
    /// 2.Model类名必须为***Config
    /// 3.Model类中变量名必须与Config中相同
    /// 5.配置表必须有id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Dictionary<int, T> InitConfigDic<T>(string fileName) where T : new()
    {
        string data = Resources.Load("Config/" + fileName).ToString();
        string[] datas = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
        List<string> typeList = new List<string>();
        List<string> nameList = new List<string>();
        List<string> variableNameList = new List<string>();
        typeList = datas[0].Split('\t').ToList();
        nameList = datas[1].Split('\t').ToList();
        variableNameList = datas[2].Split('\t').ToList();

        Dictionary<int, T> ret = new Dictionary<int, T>();

        //遍历正文内容
        for (int i = 3; i < datas.Length; i++)
        {
            if (datas[i].Length == 0 || datas[i] == null)
            {
                Debug.LogWarning(fileName + "配置表：第" + (i + 1) + "行为空或长度为0，请检查！！！");
                continue;
            }

            //实例化数据对象
            T obj = new T();
            Type t = obj.GetType();

            //获取对象的全部属性
            FieldInfo[] fields = t.GetFields();

            string[] thisData = datas[i].Split('\t');
            string errorMsg = "";
            int id = 0;
            try
            {
                //遍历全部属性，并从配置表中找出对应字段的数据并赋值
                //根据属性的类型对数据进行转换
                foreach (FieldInfo field in fields)
                {
                    if (!variableNameList.Contains(field.Name))
                    {
                        Debug.LogWarning("变量属性 [" + field.Name + "] is null !!!");
                        continue;
                    }
                    int position = variableNameList.IndexOf(field.Name);
                    string val = thisData[position];
                    if (val == null)
                    {
                        Debug.Log("变量属性 [" + field.Name + "] 的值 is null !!!");
                        continue;
                    }

                    if (field.Name == "id")
                    {
                        id = int.Parse(val);
                    }

                    errorMsg = field.Name + " : " + val + "   type : " + field.FieldType;

                    switch (typeList[position])
                    {
                        case "int":
                            field.SetValue(obj, int.Parse(val));
                            break;
                        case "double":
                            field.SetValue(obj, double.Parse(val));
                            break;
                        case "float":
                            field.SetValue(obj, float.Parse(val));
                            break;
                        case "long":
                            field.SetValue(obj, long.Parse(val));
                            break;
                        case "string":
                            field.SetValue(obj, val);
                            break;
                        case "enum":
                            //枚举类型需要在type位置写enum，在变量名位置写对应名字，导致本应该写在type处的定义类型没有位置写
                            /*
                             * 统一约定枚举变量命名规则如下：
                             * 1.类型定义一定为***Enum
                             * 2.变量名一定为 x****   (其中x代表首字母小写)
                            */
                            string enumTypeName = variableNameList[position];
                            enumTypeName = enumTypeName.Substring(0, 1).ToUpper() + enumTypeName.Substring(1, enumTypeName.Length - 1) + "Enum";

                            Type thisType = Type.GetType(enumTypeName);
                            field.SetValue(obj, Enum.Parse(thisType, val));
                            break;

                        //数组用default，一下为1-3维的int、double数组实现，如需其他数组，可参照添加相关代码
                        default:
                            if (!typeList[position].Contains("["))
                            {
                                Debug.LogError(fileName + "读取时，" + typeList[position] + " 类型未被包含，请修改或前往ConfigReader添加！！！");
                                break;
                            }
                            else
                            {
                                //通过 , 判断维度
                                int weiDu = 1; //数组维度
                                char[] tempCharArr = typeList[position].ToCharArray();
                                foreach (char c in tempCharArr)
                                    if (c == ',')
                                        weiDu++;

                                string typeName = typeList[position].Replace("[", "").Replace(",", "").Replace("]", "");

                                string first = "System.";
                                switch (typeName)
                                {
                                    case "int":
                                        typeName = first + typeName.Substring(0, 1).ToUpper() + typeName.Substring(1, typeName.Length - 1)+"32";
                                        break;
                                    case "double":
                                        typeName = first + typeName.Substring(0, 1).ToUpper() + typeName.Substring(1, typeName.Length - 1);
                                        break;
                                    default:
                                        Debug.LogError("数组类型转换失败！！！！");
                                        break;
                                }

                                Type type = Type.GetType(typeName);
                                Array arr = null;
                                string[] valOne = null;
                                string[] valTwo = null;
                                string[] valThr = null;
                                //暂时支持三维数组
                                //一维之间用 + 隔开，二维之间用 | 隔开，三维之间用 * 隔开
                                switch (weiDu)
                                {
                                    case 1:
                                        valOne = val.Split('+');
                                        arr = Array.CreateInstance(type, valOne.Length);
                                        break;
                                    case 2:
                                        valOne = val.Split('|');
                                        valTwo = valOne[0].Split('+');
                                        arr = Array.CreateInstance(type, valOne.Length, valTwo.Length);
                                        break;
                                    case 3:
                                        valOne = val.Split('*');
                                        valTwo = valOne[0].Split('|');
                                        valThr = valTwo[0].Split('+');
                                        arr = Array.CreateInstance(type, valOne.Length, valTwo.Length, valThr.Length);
                                        break;
                                    default:
                                        Debug.LogError("数组维数超过三维，暂不支持！！！");
                                        break;
                                }
                                switch (typeName)
                                {
                                    case "System.Int32":
                                        switch (weiDu)
                                        {
                                            case 1:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    arr.SetValue(int.Parse(valOne[j]), j);
                                                }
                                                field.SetValue(obj, (int[])arr);
                                                break;
                                            case 2:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    valTwo = valOne[j].Split('+');
                                                    for (int m = 0; m < valTwo.Length; m++)
                                                    {
                                                        arr.SetValue(int.Parse(valTwo[m]), j, m);
                                                    }
                                                }
                                                field.SetValue(obj, (int[,])arr);
                                                break;
                                            case 3:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    valTwo = valOne[j].Split('|');
                                                    for (int m = 0; m < valTwo.Length; m++)
                                                    {
                                                        valThr = valTwo[m].Split('+');
                                                        for (int n = 0; n < valThr.Length; n++)
                                                        {
                                                            arr.SetValue(int.Parse(valThr[n]), j, m, n);
                                                        }
                                                    }
                                                }
                                                field.SetValue(obj, (int[,,])arr);
                                                break;
                                        }
                                        break;
                                    case "System.Double":
                                        switch (weiDu)
                                        {
                                            case 1:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    arr.SetValue(double.Parse(valOne[j]), j);
                                                }
                                                field.SetValue(obj, (double[])arr);
                                                break;
                                            case 2:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    valTwo = valOne[j].Split('+');
                                                    for (int m = 0; m < valTwo.Length; m++)
                                                    {
                                                        arr.SetValue(double.Parse(valTwo[m]), j, m);
                                                    }
                                                }
                                                field.SetValue(obj, (double[,])arr);
                                                break;
                                            case 3:
                                                for (int j = 0; j < valOne.Length; j++)
                                                {
                                                    valTwo = valOne[j].Split('|');
                                                    for (int m = 0; m < valTwo.Length; m++)
                                                    {
                                                        valThr = valTwo[m].Split('+');
                                                        for (int n = 0; n < valThr.Length; n++)
                                                        {
                                                            arr.SetValue(double.Parse(valThr[n]), j, m, n);
                                                        }
                                                    }
                                                }
                                                field.SetValue(obj, (double[,,])arr);
                                                break;
                                        }
                                        break;
                                    default:
                                        Debug.LogError("Array转对应数组是出现问题，请检测是类型不支持还是数据错误！！！");
                                        break;
                                }
                            }
                            break;
                    }
                }
                ret.Add(id, obj);
            }
            catch (Exception e)
            {
                Debug.LogError("=====================" + fileName + "==================");
                Debug.LogError(e.Message);
                Debug.LogError(errorMsg);
            }
        }
        return ret;
    }

    #endregion

    #region 配置表查询

    /// <summary>
    /// 通过配置表泛型+id查找对应Model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <returns></returns>
    public T GetConfigById<T>(int id) where T : new()
    {
        IDictionary idic;
        T obj = new T();
        Type t = obj.GetType();
        string configName = t.FullName;
        if (configDic.TryGetValue(configName, out idic))
        {
            foreach (DictionaryEntry de in idic)
            {
                if ((int)(de.Key) == id)
                    return (T)(de.Value);
            }
        }
        return default(T);
    }

    /// <summary>
    /// 通过泛型返回这个泛型的字典
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Dictionary<int,T> GetConfigDic<T>() where T : new()
    {
        T obj = new T();
        Type t = obj.GetType();
        string configName = t.FullName;

        Dictionary<int, T> dic;
        IDictionary idic;
        if(configDic.TryGetValue(configName,out idic))
        {
            dic = new Dictionary<int, T>();
            foreach(DictionaryEntry de in idic)
            {
                dic.Add((int)(de.Key), (T)(de.Value));
            }
            return dic;
        }

        return null;
    }


    public List<T> GetConfigList<T>() where T : new()
    {
        T obj = new T();
        Type t = obj.GetType();
        string configName = t.FullName;

        List<T> list;
        IDictionary idic;
        if (configDic.TryGetValue(configName, out idic))
        {
            list = new List<T>();
            foreach (DictionaryEntry de in idic)
            {
                list.Add((T)(de.Value));
            }
            return list;
        }
        return null;
    }
    #endregion
}
