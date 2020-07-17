using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardTypeEnum : byte
{
    None = 0,
    Tank = 1,
    DPS = 2,
    Assist = 3
}
public enum CardQualityEnum : byte
{
    None = 0,
    Green = 1,
    Blue = 2,
    Purple = 3,
    Orange = 4
}
/// <summary>
/// 卡牌数据模型
/// </summary>
public class CardConfig
{
    //quality:品级 1=>绿卡 2=>蓝卡 3=>紫卡 4=>橙卡
    public int id;
    public string name;
    public int[,] quality;
    public string icon;
    public CardTypeEnum cardType;
    public double[] test;
}
