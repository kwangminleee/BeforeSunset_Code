using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private const float ORE_INTERACT_RANGE = 1.5F;

    private BasePlayer _player;
    private PlayerInputActions _actions;
    private PlayerEffect _playerEffect;
    private EquipmentDatabase _equippedPickaxe;
    private PlayerInputHandler _inputHandler;

    private Vector2 _moveDir;
    private Vector3 _clickDir;

    private Coroutine _swingCoroutine;
    private Coroutine _swingLoopCoroutine;
    private bool _isSwingButtonHeld = false;
    private bool _wasPointerOverUIOnSwingStart = false;

    private bool _isDashing = false;
    private float _lastDashTime = -10f;
    private Vector2 _dashDirection;

    private bool _isEnterPortal;

    private bool _isSwing => _swingCoroutine != null;
    private bool _isRecalling => PlayerInputHandler._isRecallInProgress;

    #region Event Subscriptions
    private void OnMoveStarted(InputAction.CallbackContext context)
    {
        if (PlayerInputHandler._isRecallInProgress || _isDashing || _isEnterPortal) return;

        _player.Animator.SetBool(BasePlayer.MOVE, true);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (_isDashing || _isEnterPortal) return;

        Vector2 newMoveDir = context.ReadValue<Vector2>().normalized;

        // 움직임이 시작되면 귀환 취소
        if (newMoveDir != Vector2.zero && PlayerInputHandler._isRecallInProgress)
        {
            _inputHandler.CancelRecall();
        }

        _moveDir = newMoveDir;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (_isDashing) return;
        _moveDir = Vector2.zero;
        _player.Animator.SetBool(BasePlayer.MOVE, false);
    }

    private void OnDashStarted(InputAction.CallbackContext context)
    {
        if (_isRecalling || _isDashing || BuildManager.Instance.IsPlacing || _isEnterPortal) return;

        // 쿨타임 체크
        if (Time.time - _lastDashTime < _player.Stat.DashCooldown) return;

        // 대시 귀환 취소
        if (PlayerInputHandler._isRecallInProgress)
        {
            _inputHandler.CancelRecall();
            if (PlayerInputHandler._isRecallInProgress) return;
        }

        TryDash();
    }

    private void OnSwingStarted(InputAction.CallbackContext context)
    {
        if (_isRecalling || BuildManager.Instance.IsPlacing || _isDashing || _isEnterPortal) return;

        // 스윙 귀환 취소
        if (PlayerInputHandler._isRecallInProgress)
        {
            _inputHandler.CancelRecall();
            if (PlayerInputHandler._isRecallInProgress) return;
        }

        _isSwingButtonHeld = true;

        if (_player.Animator.GetFloat(BasePlayer.MINING) != (_player.Stat.MiningSpeed / 100f))
            _player.Animator.SetFloat(BasePlayer.MINING, (_player.Stat.MiningSpeed / 100f));

        // 첫 번째 스윙 실행
        if (_swingCoroutine == null)
        {
            _swingCoroutine = StartCoroutine(C_Swing());
        }

        // 연속 스윙 코루틴 시작
        if (_swingLoopCoroutine == null)
        {
            _swingLoopCoroutine = StartCoroutine(C_SwingLoop());
        }
    }

    private void OnSwingCanceled(InputAction.CallbackContext context)
    {
        _isSwingButtonHeld = false;

        // 연속 스윙 코루틴 중지
        if (_swingLoopCoroutine != null)
        {
            StopCoroutine(_swingLoopCoroutine);
            _swingLoopCoroutine = null;
        }
    }
    #endregion

    #region Event Unsubscriptions
    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Move.started -= OnMoveStarted;
        _actions.Player.Move.performed -= OnMovePerformed;
        _actions.Player.Move.canceled -= OnMoveCanceled;
        _actions.Player.Swing.started -= OnSwingStarted;
        _actions.Player.Swing.canceled -= OnSwingCanceled;
        _actions.Player.Dash.started -= OnDashStarted;
    }
    #endregion

    private void FixedUpdate()
    {
        if (_isSwing || _isRecalling) return;

        if (!_isDashing && !_isEnterPortal)
        {
            if (_moveDir != Vector2.zero)
            {
                _player.Animator.SetBool(BasePlayer.MOVE, true);
                SetAnimationDirection(_moveDir);
            }
            else
            {
                _player.Animator.SetBool(BasePlayer.MOVE, false);
            }

            Move();
        }
    }

    /// <summary>
    /// 플레이어 컨트롤러 초기화 + 인풋 시스템 이벤트 등록
    /// </summary>
    public void Init(BasePlayer player)
    {
        _player = player;
        _playerEffect = GetComponent<PlayerEffect>();
        _equippedPickaxe = player.Stat.Pickaxe;
        _inputHandler = GetComponent<PlayerInputHandler>(); // 추가

        _actions = player.InputActions;
        _actions.Player.Move.started += OnMoveStarted;
        _actions.Player.Move.performed += OnMovePerformed;
        _actions.Player.Move.canceled += OnMoveCanceled;
        _actions.Player.Swing.started += OnSwingStarted;
        _actions.Player.Swing.canceled += OnSwingCanceled;
        _actions.Player.Dash.started += OnDashStarted;
        _actions.Player.Enable();
    }

    /// <summary>
    /// 플레이어 이동
    /// </summary>
    private void Move()
    {
        SetAnimationDirection(_moveDir);
        _player.Rigid.MovePosition(_player.Rigid.position + _moveDir * Time.fixedDeltaTime * _player.Stat.MoveSpeed);
    }

    /// <summary>
    /// 대시
    /// </summary>
    private void TryDash()
    {
        if (_moveDir != Vector2.zero)
        {
            _dashDirection = _moveDir.normalized;
        }
        else
        {
            // 현재 애니메이터의 방향 값을 가져와서 대시 방향 설정
            float x = _player.Animator.GetFloat(BasePlayer.X);
            float y = _player.Animator.GetFloat(BasePlayer.Y);
            _dashDirection = new Vector2(x, y).normalized;
        }

        StartCoroutine(C_Dash());
    }

    /// <summary>
    /// 대시 실행 코루틴
    /// </summary>
    private IEnumerator C_Dash()
    {
        _isDashing = true;
        _lastDashTime = Time.time;

        // 대시 애니메이션 방향 설정
        SetAnimationDirection(_dashDirection);

        // 대시 사운드
        AudioManager.Instance.PlaySFX("Dash");

        // 대시 이펙트
        _playerEffect.PlayDashEffect(_player.Stat.DashDuration);

        int playerLayer = LayerMask.NameToLayer("Player");
        int towerLayer = LayerMask.NameToLayer("Tower");
        int smelterLayer = LayerMask.NameToLayer("Smelter");

        Physics2D.IgnoreLayerCollision(playerLayer, towerLayer, true);
        Physics2D.IgnoreLayerCollision(playerLayer, smelterLayer, true);

        float elapsed = 0f;
        Vector2 startPos = _player.Rigid.position;

        while (elapsed < _player.Stat.DashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            // 대시 이동
            Vector2 dashMovement = _dashDirection * _player.Stat.DashSpeed * Time.fixedDeltaTime;
            _player.Rigid.MovePosition(_player.Rigid.position + dashMovement);

            yield return new WaitForFixedUpdate();
        }

        Physics2D.IgnoreLayerCollision(playerLayer, towerLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, smelterLayer, false);

        _isDashing = false;
        Vector2 currentInput = _actions.Player.Move.ReadValue<Vector2>().normalized;
        _moveDir = currentInput;

        if (_moveDir != Vector2.zero)
        {
            _player.Animator.SetBool(BasePlayer.MOVE, true);
            SetAnimationDirection(_moveDir);
        }
        else
        {
            _player.Animator.SetBool(BasePlayer.MOVE, false);
        }
    }

    /// <summary>
    /// 연속 스윙 처리
    /// </summary>
    private IEnumerator C_SwingLoop()
    {
        // 첫 번째 스윙이 끝날 때까지 대기
        yield return new WaitUntil(() => _swingCoroutine == null);

        while (_isSwingButtonHeld)
        {
            if (_isRecalling || _isDashing)
            {
                break;
            }

            // 다음 스윙 실행
            _swingCoroutine = StartCoroutine(C_Swing());

            // 현재 스윙이 끝날 때까지 대기
            yield return new WaitUntil(() => _swingCoroutine == null);
        }

        _swingLoopCoroutine = null;
    }

    /// <summary>
    /// 스윙 애니메이션 실행
    /// </summary>
    private IEnumerator C_Swing()
    {
        yield return new WaitForEndOfFrame();

        // UI 클릭 체크
        if (EventSystem.current.IsPointerOverGameObject())
        {
            _swingCoroutine = null;
            yield break;
        }

        if (_wasPointerOverUIOnSwingStart)
        {
            _swingCoroutine = null;
            yield break;
        }

        if (_player.IsInBase)
        {
            _swingCoroutine = null;
            yield break;
        }

        // 클릭 월드 포지션 구하기
        Vector3 clickPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        clickPos.z = 0f;

        IInteractable target = InteractManager.Instance.GetCurrentTarget();

        if (target != null && !(target is OreController))
        {
            target = null;
        }

        // 플레이어 기준으로 클릭의 방향 벡터 구하기
        _clickDir = (clickPos - _player.Animator.transform.position).normalized;
        _clickDir = Mathf.Abs(_clickDir.x) > Mathf.Abs(_clickDir.y)
            ? Vector2.right * Mathf.Sign(_clickDir.x)
            : Vector2.up * Mathf.Sign(_clickDir.y);

        _player.Animator.SetFloat(BasePlayer.X, _clickDir.x);
        _player.Animator.SetFloat(BasePlayer.Y, _clickDir.y);
        _player.Animator.SetTrigger(BasePlayer.SWING);

        // 채광 효과음
        if (target == null || !(target is OreController) && !target.IsInteractable(_player.transform.position, 5f, _player.PlayerCollider))
        {
            // 헛스윙
            AudioManager.Instance.PlaySFX("SwingMiss");
        }
        else
        {
            AudioManager.Instance.PlayRandomSFX("HittingARock", 4);
        }

        // 애니메이션 끝나는 걸 기다렸다가 채광 시도
        yield return Helper_Coroutine.WaitSeconds(0.5f / (_player.Stat.MiningSpeed / 100f));

        TryInteractTarget(target);

        _swingCoroutine = null;
    }

    /// <summary>
    /// 채광 시도
    /// </summary>
    private void TryInteractTarget(IInteractable target)
    {
        if (InteractManager.Instance.IsPointerOverRealUI()) return;

        if (target == null)
        {
            ToastManager.Instance.ShowToast("목표가 존재하지 않습니다.");
            return;
        }

        if (!target.IsInteractable(_player.transform.position, ORE_INTERACT_RANGE, _player.PlayerCollider)) return;
        target.Interact();
    }

    /// <summary>
    /// 애니메이션 방향 설정
    /// </summary>
    private void SetAnimationDirection(Vector2 moveDir)
    {
        if (moveDir == Vector2.zero) return;

        if (Mathf.Abs(moveDir.x) == Mathf.Abs(moveDir.y))
        {
            moveDir.x = moveDir.x > 0 ? 1f : -1f;
            moveDir.y = 0f;
        }
        _player.Animator.SetFloat(BasePlayer.X, moveDir.x);
        _player.Animator.SetFloat(BasePlayer.Y, moveDir.y);
    }

    public void SetEnterPortal(bool value)
    {
        _isEnterPortal = value;
        if (value)
        {
            _moveDir = Vector2.zero;
            _player.Animator.SetBool(BasePlayer.MOVE, false);

            _actions.Player.Move.Disable();
            _actions.Player.Move.Enable();
        }
    }
}
