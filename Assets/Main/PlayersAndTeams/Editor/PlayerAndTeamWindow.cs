﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class PlayerAndTeamWindow : EditorWindow
{
    public AllPlayerManager players;                            // 玩家列表
    public List<PlayerInformation> playerInfoList;              // 玩家信息列表
    public int teamsSize = 0;                                   // 团队数量
    public List<TeamManager> teams = new List<TeamManager>();   // 团队列表

    private bool showTeamList = true;           // 是否显示团队列表
    private Vector2 scrollPos;                  // 滑动面板位置
    private GUIContent content;                 // 临时GUI内容
    private bool allAIEnableTrigger;

    [MenuItem("Window/Player And Team")]
    static void ShowWindows()
    {
        EditorWindow window = GetWindow<PlayerAndTeamWindow>();
        window.minSize = new Vector2(500f, 280f);
        window.Show();
    }

    private void OnGUI()
    {
        if (!GetPlayers())
            return;
        SelectedMyPlayer();
        GetTeams();
        GUILayout.Label("============================================================================================================");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        SetAllAIEnableTrigger();
        PlayersAndTeamsOperation();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 获取玩家列表，强制重置玩家ID
    /// </summary>
    /// <returns>成功返回ture</returns>
    private bool GetPlayers()
    {
        players = EditorGUILayout.ObjectField("All Players", players, typeof(AllPlayerManager), false) as AllPlayerManager;
        if (players == null)
            return false;
        playerInfoList = players.playerInfoList;
        if (playerInfoList == null || playerInfoList.Count <= 0)
        {
            EditorGUILayout.HelpBox("Waring : playerInfoList Is Empty", MessageType.Warning);
            return false;
        }
        for (int i = 0; i < playerInfoList.Count; i++)
            playerInfoList[i].id = i;
        return true;
    }

    /// <summary>
    /// 选择我的玩家
    /// </summary>
    private void SelectedMyPlayer()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Select My Player : ");
        if (EditorGUILayout.DropdownButton(GetContentByPlayerIndex(players.myPlayerIndex), FocusType.Passive))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), false, () => { players.myPlayerIndex = -1; });
            for (int i = 0; i < playerInfoList.Count; i++)
            {
                if (players.myPlayerIndex == i)
                    continue;
                menu.AddItem(GetContentByPlayerIndex(i), false, (object index) => { players.myPlayerIndex = (int)index; }, i);
            }
            menu.ShowAsContext();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 获取索引对应玩家内容信息（ID 和 名字），失败为空
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns>返回信息</returns>
    private GUIContent GetContentByPlayerIndex(int index)
    {
        content = new GUIContent();
        if (GameMathf.ValueInRange(0, playerInfoList.Count - 1, index))
            content.text = string.Format("ID : {0}   Name : {1}", playerInfoList[index].id, playerInfoList[index].name);
        return content;
    }

    /// <summary>
    /// 获取团队列表，并且重置ID
    /// </summary>
    private void GetTeams()
    {
        showTeamList = EditorGUILayout.Foldout(showTeamList, "Team List");
        if (!showTeamList)
            return;
        EditorGUI.indentLevel = 1;
        teamsSize = EditorGUILayout.IntField("Size", teamsSize);
        if (teams.Count > teamsSize)                                        // 如果大小改小了，删掉
            teams.RemoveRange(teamsSize, teams.Count - teamsSize);
        for (int i = 0; i < teamsSize; i++)
        {
            if (teams.Count < teamsSize)                                    // 如果大了，添加
                teams.Add(null);
            teams[i] = EditorGUILayout.ObjectField("Team " + i, teams[i], typeof(TeamManager), false) as TeamManager;
            if (teams[i] != null)
            teams[i].TeamID = i;
        }
        EditorGUI.indentLevel = 0;
    }

    /// <summary>
    /// 设置所有玩家AI是否激活
    /// </summary>
    private void SetAllAIEnableTrigger()
    {
        if (!GUILayout.Button("Set All AI Enable/Disable"))
            return;
        allAIEnableTrigger = !allAIEnableTrigger;
        for (int i = 0; i < playerInfoList.Count; i++)
            playerInfoList[i].isAI = allAIEnableTrigger;
    }

    /// <summary>
    /// 玩家和团队选择列表
    /// </summary>
    private void PlayersAndTeamsOperation()
    {
        for (int i = 0; i < playerInfoList.Count; i++)
        {
            EditorGUILayout.BeginVertical("Box");
            PlayerOperation(i);
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 单个玩家管理
    /// </summary>
    /// <param name="index">当前玩家索引</param>
    private void PlayerOperation(int index)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(" ID : " + playerInfoList[index].id, GUILayout.Width(50));

        EditorGUILayout.LabelField(" Name : " + playerInfoList[index].name, GUILayout.Width(120));

        EditorGUILayout.LabelField(" Join : ", GUILayout.Width(30));
        playerInfoList[index].isJoin = EditorGUILayout.Toggle(playerInfoList[index].isJoin, GUILayout.Width(50));

        EditorGUILayout.LabelField(" AI : ", GUILayout.Width(30));
        playerInfoList[index].isAI = EditorGUILayout.Toggle(playerInfoList[index].isAI, GUILayout.Width(50));

        EditorGUILayout.LabelField(" Perfab : ", GUILayout.Width(55));
        playerInfoList[index].assembleTank = EditorGUILayout.ObjectField(playerInfoList[index].assembleTank, typeof(TankAssembleManager), false, GUILayout.Width(125)) as TankAssembleManager;

        EditorGUILayout.LabelField(" Team : ", GUILayout.Width(30));
        TeamSelect(index);                                    //团队显示下拉菜单

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 显示团队下拉列表，第一项是玩家已经有队伍
    /// </summary>
    /// <param name="index">玩家索引</param>
    private void TeamSelect(int index)
    {
        content = new GUIContent();
        //如果存在该队伍，直接显示在下拉菜单中
        if (playerInfoList[index].team != null)
            content.text = playerInfoList[index].team.TeamName;

        //下拉菜单，显示可选择队伍，修改后对应也会修改teamsManager
        if (EditorGUILayout.DropdownButton(content, FocusType.Passive))
        {
            GenericMenu menu = new GenericMenu();
            // 添加一条空的，就是可以不选择任何队伍
            menu.AddItem(new GUIContent("None (No Team)"), false,() => { playerInfoList[index].team = null; });
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i] == null || playerInfoList[index].team == teams[i])
                    continue;
                menu.AddItem(new GUIContent(teams[i].TeamName), false, (object teamIndex) => {playerInfoList[index].team = teams[(int)teamIndex]; },i);
            }
            menu.ShowAsContext();
        }
    }

}
