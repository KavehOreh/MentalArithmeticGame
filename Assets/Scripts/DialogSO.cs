using System;              // Подключаем системные классы для работы с Serializable
using System.Collections.Generic;  // Подключаем коллекции (List)
using UnityEngine;         // Подключаем основные классы Unity

// Атрибут [Serializable] позволяет видеть этот класс в инспекторе Unity
[Serializable]
public class DialogData
{
    public string speakerName;     // Имя персонажа, который говорит в этой части диалога
    [TextArea(3, 5)]               // Атрибут создает многострочное поле в инспекторе (минимум 3 строки, максимум 5)
    public string[] sentences;      // Массив строк с репликами этого персонажа
}

// CreateAssetMenu позволяет создавать объект диалога через меню Create
[CreateAssetMenu(fileName = "NewDialog", menuName = "Dialog System/Dialog")]
public class DialogSO : ScriptableObject  // ScriptableObject - специальный тип для хранения данных вне сцены
{
    public List<DialogData> dialogParts;  // Список частей диалога (может быть несколько персонажей)
    public bool isCutscene = false;        // Флаг, является ли этот диалог частью кат-сцены
}