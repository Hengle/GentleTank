﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CurrentTankPanelManager : MonoBehaviour 
{
    public GameObject deleteConfirmPanel;                           // 删除确认窗口
    public Toast toast;                                             // 提示吐司
    public Button deleteButton;                                     // 删除按钮
    public Button finishedButton;                                   // 完成按钮
    public AllCustomTankManager allCustomTank;                      // 所有坦克管理器
    public TankAssembleManager defaultTankAssemble;                 // 默认坦克组装（用来创建）
    //public UnityEvent createdSuccessedEvent;                        // 创建成功事件
    //public UnityEvent selectSuccessedEvent;                         // 选择成功事件

    private TankAssembleManager newTankAssemble;                    // 新建的坦克组装

    /// <summary>
    /// 创建新的坦克
    /// </summary>
    public void CreateNewTank()
    {
        if (allCustomTank.Count >= AllCustomTankManager.MaxSize)
        {
            toast.ShowToast("坦克库已满。");
            return;
        }
        allCustomTank.AddNewTank();
        allCustomTank.CatchTankTexture(allCustomTank.Count - 1);
        allCustomTank.SelectCurrentTank(allCustomTank.Count - 1);
        allCustomTank.OnTankPreviewClicked();
        //createdSuccessedEvent.Invoke();
    }

    /// <summary>
    /// 删除当前坦克
    /// </summary>
    public void DeleteCurrentTank()
    {
        if (allCustomTank.CurrentTank == null)
            return;
        if (allCustomTank.Count <= 1)
        {
            toast.ShowToast("至少留下一部坦克。");
            return;
        }
        allCustomTank.DeleteCurrentTank();
        allCustomTank.SetupAllTankTexture();
        OnTankSelected();
    }

    /// <summary>
    /// 选择当前坦克
    /// </summary>
    public void SelectedCurrentTank()
    {
        if (!allCustomTank.SetCurrentTankToMaster())
            return;
        //if (MasterManager.Instance.data.weightLimit < allCustomTank.CurrentTankAssemble.GetTotalWeight())
        //{
        //    Toast.Instance.ShowToast("超出承重。");
        //    return;
        //}
        //selectSuccessedEvent.Invoke();
    }

    /// <summary>
    /// 选择坦克时响应
    /// </summary>
    public void OnTankSelected()
    {
        deleteButton.interactable = allCustomTank.CurrentTank == null ? false : true;
        //finishedButton.interactable = allCustomTank.CurrentTank == null ? false : true;
    }

}
