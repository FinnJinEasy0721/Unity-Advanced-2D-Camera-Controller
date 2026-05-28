using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("跟随参数")]
    [Tooltip("水平跟随平滑时间（越小越灵敏）")]
    public float smoothTimeX = 0.06f;

    [Tooltip("垂直跟随平滑时间（略大于水平，跳跃更自然）")]
    public float smoothTimeY = 0.15f;

    [Tooltip("摄像机最大移动速度")]
    public float maxSpeed = 50f;

    [Header("水平前视偏移 (Look-ahead)")]
    [Tooltip("AD键方向的水平前视距离")]
    public float lookAheadDistX = 2f;

    [Tooltip("前视偏移移动速度（匀速，不过冲）")]
    public float lookAheadSpeed = 12f;

    [Header("跳跃镜头偏移")]
    [Tooltip("跳跃上升时摄像机上移距离")]
    public float jumpLookUpDist = 2.5f;

    [Tooltip("下落时镜头偏移回收的速度倍率")]
    public float fallLookCancelSpeed = 4f;

    [Header("滑行镜头偏移")]
    [Tooltip("滑行时摄像机下移距离")]
    public float slideLookDownDist = 1.5f;

    [Header("镜头偏移速度")]
    [Tooltip("镜头偏移的移动速度（匀速，不过冲）")]
    public float verticalLookSpeed = 12f;

    [Header("基础偏移")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    // X/Y 独立 SmoothDamp 速度
    private float velocityX;
    private float velocityY;

    // 前视偏移
    private float lookAheadOffsetX;

    // 垂直镜头偏移
    private float verticalLookOffset;

    // 震动偏移
    private Vector3 shakeOffset;

    // 玩家引用
    private PlayerController playerController;
    private PlayerStateMachine stateMachine;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 懒加载引用
        if (playerController == null)
            playerController = target.GetComponent<PlayerController>();
        if (stateMachine == null && playerController != null)
            stateMachine = playerController.GetComponent<PlayerStateMachine>();

        // 水平前视偏移（AD键 + 滑行朝向）
        float targetLookAhead;
        if (stateMachine != null && stateMachine.currentState == PlayerStateMachine.PlayerState.Slide)
        {
            // 滑行时用朝向驱动（玩家滑行中不一定按着方向键）
            float facingDir = Mathf.Sign(target.localScale.x);
            targetLookAhead = facingDir * lookAheadDistX;
        }
        else
        {
            // 其他状态用AD键输入驱动
            float horizontalInput = playerController != null ? playerController.GetHorizontalInput() : 0f;
            targetLookAhead = horizontalInput * lookAheadDistX;
        }

        lookAheadOffsetX = Mathf.MoveTowards(
            lookAheadOffsetX,
            targetLookAhead,
            lookAheadSpeed * Time.deltaTime
        );

        // 垂直镜头偏移（玩家状态驱动）
        float targetVerticalLook = 0f;
        float vertSpeed = verticalLookSpeed;

        if (stateMachine != null)
        {
            var state = stateMachine.currentState;

            if (state == PlayerStateMachine.PlayerState.Jump)
            {
                targetVerticalLook = jumpLookUpDist;
            }
            else if (state == PlayerStateMachine.PlayerState.Fall)
            {
                // 下落时偏移归零，加速回收让镜头跟随玩家下落
                targetVerticalLook = 0f;
                if (verticalLookOffset > 0.01f)
                    vertSpeed *= fallLookCancelSpeed;
            }
            else if (state == PlayerStateMachine.PlayerState.Slide)
            {
                targetVerticalLook = -slideLookDownDist;
            }
        }

        verticalLookOffset = Mathf.MoveTowards(
            verticalLookOffset,
            targetVerticalLook,
            vertSpeed * Time.deltaTime
        );

        // 目标位置
        Vector3 targetPos = target.position + offset;
        targetPos.x += lookAheadOffsetX;
        targetPos.y += verticalLookOffset;
        targetPos.z = offset.z;

        // X/Y 独立平滑跟随
        float smoothX = Mathf.SmoothDamp(
            transform.position.x,
            targetPos.x,
            ref velocityX,
            smoothTimeX,
            maxSpeed
        );

        float smoothY = Mathf.SmoothDamp(
            transform.position.y,
            targetPos.y,
            ref velocityY,
            smoothTimeY,
            maxSpeed
        );

        // 到达阈值：足够接近时直接吸附，消除微抖
        float snapThreshold = 0.005f;
        if (Mathf.Abs(smoothX - targetPos.x) < snapThreshold)
        {
            smoothX = targetPos.x;
            velocityX = 0f;
        }
        if (Mathf.Abs(smoothY - targetPos.y) < snapThreshold)
        {
            smoothY = targetPos.y;
            velocityY = 0f;
        }

        Vector3 finalPos = new Vector3(smoothX, smoothY, offset.z) + shakeOffset;
        transform.position = finalPos;
    }

    /// <summary>
    /// 摄像机震动
    /// </summary>
    public void CameraShake(int Count, float Speed, float Strength)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(Count, Speed, Strength));
    }

    private IEnumerator ShakeCoroutine(int count, float speed, float strength)
    {
        for (int i = 0; i < count; i++)
        {
            shakeOffset = (Vector3)Random.insideUnitCircle * strength;
            yield return new WaitForSeconds(1f / speed);
            shakeOffset = Vector3.zero;
            yield return new WaitForSeconds(1f / speed);
        }

        shakeOffset = Vector3.zero;
    }
}
