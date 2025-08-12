using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private BasePlayer _player;
    private PlayerInputActions _actions;

    private bool _isRecallStarted;
    private bool _isForcedRecall = false;

    public static bool _isRecallInProgress { get; private set; } = false;

    #region Event Subscriptions
    private void OnInventoryStarted(InputAction.CallbackContext context)
    {
        InventoryManager.Instance.Inventory.Toggle();
    }

    private void OnBuildStarted(InputAction.CallbackContext context)
    {
        UIManager.Instance.CraftArea.Toggle();
    }

    private void OnDestroyModeStarted(InputAction.CallbackContext context)
    {
        if (UIManager.Instance.UpgradeUI.isActiveAndEnabled) return;

        BuildManager.Instance.IsOnDestroy = !BuildManager.Instance.IsOnDestroy;
        UIManager.Instance.DestroyModeUI.SetMode(BuildManager.Instance.IsOnDestroy);
        //TODO UIManager에서 철거버튼 색상 바꿔주기
        InteractManager.Instance.SetCursorDestroyImage(BuildManager.Instance.IsOnDestroy);
    }

    private void OnReturnHomeStarted(InputAction.CallbackContext context)
    {
        // 바로 귀환 시작
        if (!_player.IsInBase && !_isRecallStarted)
        {
            StartRecall();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Vector3 clickWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        clickWorldPos.z = 0f;

        // interactable 레이어에서 충돌체 검색
        LayerMask interactableLayer = LayerMask.GetMask("Tower", "Core", "Smelter");
        Collider2D col = Physics2D.OverlapPoint(clickWorldPos, interactableLayer);

        if (col == null) return;

        IInteractable target = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();
        if (target == null)
        {
            Debug.LogWarning("IInteractable 못찾음");
            return;
        }

        if (target == null) return;

        // 플레이어 위치에서 상호작용 가능한지 검사
        if (!target.IsInteractable(_player.transform.position, 5f, _player.PlayerCollider))
            return;

        target.Interact();
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
    }
    #endregion

    #region Event Unsubscriptions
    private void OnDisable()
    {
        _actions.Interaction.Disable();
        _actions.Player.Interact.performed -= OnInteractPerformed;
        _actions.Interaction.Inventory.started -= OnInventoryStarted;
        _actions.Interaction.Build.started -= OnBuildStarted;
        _actions.Interaction.DestroyMode.started -= OnDestroyModeStarted;
        _actions.Interaction.ReturnHome.started -= OnReturnHomeStarted;
        _actions.Player.Dash.performed -= OnDashPerformed;
    }
    #endregion

    /// <summary>
    /// 키보드 인풋 핸들러 초기화
    /// </summary>
    public void Init(BasePlayer player)
    {
        _player = player;

        _actions = player.InputActions;
        _actions.Player.Interact.performed += OnInteractPerformed;
        _actions.Interaction.Inventory.started += OnInventoryStarted;
        _actions.Interaction.Build.started += OnBuildStarted;
        _actions.Interaction.DestroyMode.started += OnDestroyModeStarted;
        _actions.Interaction.ReturnHome.started += OnReturnHomeStarted;
        _actions.Player.Dash.performed += OnDashPerformed;
        _actions.Interaction.Enable();

        UIManager.Instance.RecallUI.OnCountdownFinished += OnRecallCountdownFinished;
        UIManager.Instance.RecallUI.OnRecallCanceled += OnRecallCanceled;

        _isRecallStarted = false;
    }

    // /// <summary>
    // /// R키 한 번 누르면 귀환 시작
    // /// </summary>
    // public void StartRecall()
    // {
    //     StartRecall(false);
    // }

    /// <summary>
    /// 귀환 시작 (강제 귀환 여부 지정 가능)
    /// </summary>
    public void StartRecall(bool isForced = false)
    {
        _isRecallStarted = true;
        _isRecallInProgress = true;
        _isForcedRecall = isForced;

        UIManager.Instance.RecallUI.StartRecallCountdown();
    }

    /// <summary>
    /// 귀환 취소
    /// </summary>
    public void CancelRecall()
    {
        // 강제 귀환은 리턴
        if (!_isRecallStarted || _isForcedRecall) return;

        _isRecallStarted = false;
        _isRecallInProgress = false;

        UIManager.Instance.RecallUI.CancelRecall();
    }

    /// <summary>
    /// 귀환 카운트다운이 끝나고 실제 기지로 귀환
    /// </summary>
    private void OnRecallCountdownFinished()
    {
        StartCoroutine(C_Recall());
    }

    /// <summary>
    /// 귀환이 취소되었을 때
    /// </summary>
    private void OnRecallCanceled()
    {
        _isRecallStarted = false;
        _isRecallInProgress = false;
        _isForcedRecall = false;
    }

    private IEnumerator C_Recall()
    {
        yield return StartCoroutine(ScreenFadeController.Instance.FadeInOut(() =>
        {
            UIManager.Instance.RecallUI.CloseRecall();

            if(GameManager.Instance.IsTutorial)
                _player.transform.position = Vector3.zero;
            else
                MapManager.Instance.ReturnToHomeMap();

            _isRecallStarted = false;
            _isRecallInProgress = false;
            _isForcedRecall = false;
            _player.SetPlayerInBase(true);
        }));

        QuestManager.Instance?.AddQuestAmount(QUEST_TYPE.GoToBase);
    }
}
