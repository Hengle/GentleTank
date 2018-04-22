using Item.Tank;
using Widget;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CrossPlatformInput;
using CameraRig;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct GameEvent
    {
        public UnityEvent onBeforeRoundStartEvent;
        public UnityEvent onAfterRoundStartEvent;
        public UnityEvent onBeforeRoundEndEvent;
        public UnityEvent onAfterRoundEndEvent;
        public UnityEvent onMyPlayerCreatedEvent;
        public UnityEvent onMyPlayerDeadEvent;
    }
    static public GameManager Instance { get; private set; }

    public Points spawnPoints1;
    public Points spawnPoints2;
    public Points wayPoints;
    public AllPlayerManager allPlayerManager;       // �������
    public int numRoundsToWin = 5;                  // Ӯ����Ϸ��ҪӮ�Ļغ���
    public float startDelay = 3f;                   // ��ʼ��ʱʱ��
    public float endDelay = 3f;                     // ������ʱʱ��
    public float changeCamDelay = 2f;               // ת����ͷ��ʱʱ��
    public Text messageText;                        // UI�ı�����һ�ʤ�ȣ�
    public GameEvent gameEvent;

    public TankManager MyTank { get { return myTank; } }

    private List<TankManager> tankList;             // �������̹��
    private TankManager myTank;                     // �Լ���̹��
    private WaitForSeconds startWait;               // ��ʼ�غ���ʱ
    private WaitForSeconds endWait;                 // �����غ���ʱ
    private WaitForSeconds changeCamWait;           // ת����ͷ��ʱ

    private void Awake()
    {
        Instance = this;
        tankList = new List<TankManager>();
        startWait = new WaitForSeconds(startDelay);         // ��Ϸ�غϿ�ʼ��ʱ
        endWait = new WaitForSeconds(endDelay);             // ��Ϸ�غϽ�����ʱ
        changeCamWait = new WaitForSeconds(changeCamDelay); // ��ͷת����ʱ
    }

    /// <summary>
    /// ��ʼ����Ϸ��¼ʵ������������̹�ˡ��������Ŀ�ꡢС��ͼ��ʼ������ʼ��Ϸѭ��
    /// </summary>
    private void Start()
    {
        SetupGame();                                // ������Ϸ

        GameRound.Instance.maxRound = numRoundsToWin;             // ���þ���
        GameRound.Instance.StartGame();            // ��ʼ��Ϸѭ��������ʤ�ߣ����»غϣ�������Ϸ�ȣ�
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// ��������̹�ˣ�������Һ�AI�������þ�ͷ����׷��Ŀ�ꡢС��ͼ��ʼ��
    /// </summary>
    private void SetupGame()
    {
        myTank = CreateMasterTank();
        allPlayerManager.SetupInstance();
        AllPlayerManager.Instance.CreatePlayerGameObjects(new GameObject("Tanks").transform, myTank);
        tankList.Add(myTank);
        myTank.Init(wayPoints);
        for (int i = 1; i < AllPlayerManager.Instance.Count; i++)
        {
            tankList.Add(AllPlayerManager.Instance[i].GetComponent<TankManager>());
            tankList[i].Init(wayPoints);
        }

        if (VirtualInput.GetButton("Attack") != null)
            ((ChargeButtonInput)VirtualInput.GetButton("Attack")).Setup(myTank.tankAttack, myTank.tankAttack.coolDownTime, myTank.tankAttack.minLaunchForce, myTank.tankAttack.maxLaunchForce, myTank.tankAttack.ChargeRate);

        MainCameraRig.Instance.Setup(myTank.transform, AllPlayerManager.Instance.GetAllPlayerTransform());
    }

    /// <summary>
    /// �������̹��
    /// </summary>
    private TankManager CreateMasterTank()
    {
        GameObject tank = Instantiate(MasterManager.Instance.StandardPrefab);
        MasterManager.Instance.SelectedTank.CreateTank(tank.transform);

        TankManager manager = tank.GetComponent<TankManager>();
        MasterManager.Instance.SelectedTank.InitTankComponents(manager);

        MasterData data = MasterManager.Instance.data;
        manager.Information = new PlayerInformation(0, data.masterName,data.isJoin, data.isAI, data.representColor, data.team);
        manager.stateController.defaultStats = data.aiState;

        TankHealth health = tank.GetComponent<TankHealth>();
        health.OnDeathEvent += MyTankBorkenEvent;
        gameEvent.onMyPlayerCreatedEvent.Invoke();

        return manager;
    }

    /// <summary>
    /// �Լ���̹�˻��ˣ�ת����ͷ
    /// </summary>
    private void MyTankBorkenEvent(HealthManager health, PlayerManager killer)
    {
        gameEvent.onMyPlayerDeadEvent.Invoke();
        if (killer == null)
            MainCameraRig.Instance.currentType = MainCameraRig.Type.MultiTargets;
        else
            StartCoroutine(MyTankDeathCameraBlend(health.transform, killer.transform));
    }

    /// <summary>
    /// ���������ȰѾ�ͷ��ɱ���ǵ���ң���ת����Ŀ�꾵ͷ
    /// </summary>
    /// <param name="master"></param>
    /// <param name="killer"></param>
    /// <returns></returns>
    private IEnumerator MyTankDeathCameraBlend(Transform master, Transform killer)
    {
        MainCameraRig.Instance.oneTarget = killer;
        MainCameraRig.Instance.currentType = MainCameraRig.Type.OneTarget;
        yield return changeCamWait;
        MainCameraRig.Instance.currentType = MainCameraRig.Type.MultiTargets;
        MainCameraRig.Instance.oneTarget = master;
    }

    /// <summary>
    /// ��������̹�˳�����
    /// </summary>
    private void ResetAllTanksToSpawnPoint()
    {
        int t1 = 0, t2 = 0;
        for (int i = 0; i < tankList.Count; i++)
        {
            if (tankList[i].Team.TeamID == (GameRound.Instance.CurrentRound % 2))
                tankList[i].ResetToSpawnPoint(spawnPoints1.GetWorldSpacePoint(spawnPoints1[t1++]));
            else
                tankList[i].ResetToSpawnPoint(spawnPoints2.GetWorldSpacePoint(spawnPoints2[t2++]));
        }
    }

    /// <summary>
    /// ����������ҿ���Ȩ
    /// </summary>
    /// <param name="enable">����״̬</param>
    private void SetTanksControlEnable(bool enable)
    {
        for (int i = 0; i < tankList.Count; i++)
            tankList[i].SetControlEnable(enable);
    }

    /// <summary>
    /// ��Ϸ��ѭ��Э��
    /// </summary>
    /// <returns></returns>
    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());           //�غϿ�ʼ����һ����ʱ
        yield return StartCoroutine(RoundPlaying());            //�غ���
        yield return StartCoroutine(RoundEnding());             //�غϽ���

        // �����������Ϸ�����¼��س��������������һ�غ�
        if (GameRound.Instance.IsEndOfTheGame())
            BackToMainScene();
        else
            StartCoroutine(GameLoop());
    }

    /// <summary>
    /// �غϿ�ʼ
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundStarting()
    {
        SetTanksControlEnable(false);                   // ����̹���ǵĿ���Ȩ
        ResetAllTanksToSpawnPoint();                    // ��������̹��λ��
        gameEvent.onBeforeRoundStartEvent.Invoke();
        GameRound.Instance.StartRound();

        messageText.text = "ROUND " + GameRound.Instance.CurrentRound;

        yield return changeCamWait;                     // ��ʱһ��ʱ��ת���ɵ�����ͷ
        if (myTank != null && !myTank.IsAI)
            MainCameraRig.Instance.currentType = MainCameraRig.Type.OneTarget;

        yield return startWait;                         // ��ʱһ��ʱ���ٿ�ʼ
        gameEvent.onAfterRoundStartEvent.Invoke();
    }

    /// <summary>
    /// �غ���
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundPlaying()
    {
        SetTanksControlEnable(true);                    // ������ҿ���Ȩ

        messageText.text = string.Empty;                // �����ʾ��Ϣ

        while (!GameRound.Instance.IsEndOfTheRound())   // �غ�û�����ͼ���
            yield return null;
    }

    /// <summary>
    /// �غϽ���
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundEnding()
    {
        gameEvent.onBeforeRoundEndEvent.Invoke();
        MainCameraRig.Instance.currentType = MainCameraRig.Type.MultiTargets;

        SetTanksControlEnable(false);                   // ������ҿ���Ȩ

        GameRound.Instance.UpdateWonData();             // ���»�ʤ����

        messageText.text = GameRound.Instance.GetEndingMessage();  // ��ȡ������Ϣ����ʾ֮

        yield return endWait;
        gameEvent.onAfterRoundEndEvent.Invoke();
    }

    /// <summary>
    /// �ص����˵�
    /// </summary>
    public void BackToMainScene()
    {
        StopAllCoroutines();
        AllSceneManager.LoadScene(AllSceneManager.GameSceneType.MainMenuScene);
    }
}