using UnityEngine;

public class Spider : MonoBehaviour {
    // setting
    public Transform[] Legs;
    public Vector2[] offset;
    public float FeetMinDistance = 0.01f;
    public float FeetMaxDistance = 0.3f;
    public float Up = 0.2f;
    public float distanceBodyToGround = 0.5f;

    //debug
    private Vector3[] posFeets;
    private Vector3[] posHits;
    private bool[] isMovingLeg;
    private float[] movingLegTimer;
    private void Awake() {
        // init
        posHits = new Vector3[Legs.Length];
        for (int i = 0; i < Legs.Length; i++) {
            posHits[i] = Legs[i].position;
        }

        posFeets = (Vector3[])posHits.Clone();
        for (int i = 0; i < posFeets.Length; i++) {
            posFeets[i].x += Random.Range(-0.1f, 0.1f);
            posFeets[i].z += Random.Range(-0.1f, 0.1f);
        }

        isMovingLeg = new bool[4];
        movingLegTimer = new float[4];
    }
    void Update() {
        // 由输入控制移动 
        SimpleMove();

        // 当前身体的高度应当为 4 足高度中值加上 offset
        float feetHigh = 0;
        foreach (var feet in posFeets) {
            feetHigh += feet.y;
        }
        feetHigh /= 4;
        Vector3 bodyPos = transform.position;
        bodyPos.y = feetHigh + distanceBodyToGround;
        transform.position = Vector3.Lerp(transform.position, bodyPos, 0.2f);

        // 取得局部坐标中的 4 足位置
        Vector3[] localFeets = new Vector3[4];
        Vector3 avgLocalFeet = Vector3.zero;
        for (int i = 0; i < Legs.Length; i++) {
            localFeets[i] = transform.InverseTransformPoint(posFeets[i]);
            avgLocalFeet += localFeets[i];
        }
        avgLocalFeet /= 4;

        // 身体向 4 足中较低的脚的方向轻微向下旋转
        if (localFeets[0].y > avgLocalFeet.y && (localFeets[0].y - avgLocalFeet.y) > 0.1f) {
            transform.Rotate(Vector3.forward, Space.Self);
            transform.Rotate(Vector3.left, Space.Self);
        }
        if (localFeets[1].y > avgLocalFeet.y && (localFeets[1].y - avgLocalFeet.y) > 0.1f) {
            transform.Rotate(Vector3.back, Space.Self);
            transform.Rotate(Vector3.left, Space.Self);
        }
        if (localFeets[2].y > avgLocalFeet.y && (localFeets[2].y - avgLocalFeet.y) > 0.1f) {
            transform.Rotate(Vector3.forward, Space.Self);
            transform.Rotate(Vector3.right, Space.Self);
        }
        if (localFeets[3].y > avgLocalFeet.y && (localFeets[3].y - avgLocalFeet.y) > 0.1f) {
            transform.Rotate(Vector3.back, Space.Self);
            transform.Rotate(Vector3.right, Space.Self);
        }

        // offset 存储预设状态下的足部 x，y 坐标
        // 射线朝局部空间的下方射出，检测碰撞点并存储（局部空间垂直向下，可能世界空间中是斜向下）
        for (int i = 0; i < Legs.Length; i++) {
            Vector3 point = new Vector3(offset[i].x, 1, offset[i].y);
            var hitInfos = Physics.RaycastAll(transform.TransformPoint(point), transform.TransformDirection(Vector3.down));

            Vector3 highestPoint = Vector3.down;
            foreach (var hitInfo in hitInfos) {
                Spider spider = hitInfo.transform.GetComponentInParent<Spider>();
                if (spider == null || spider != this) {
                    if (hitInfo.point.y > highestPoint.y) {
                        highestPoint = hitInfo.point;
                    }
                }
            }
            posHits[i] = highestPoint;
        }

        // 判断每个脚是否适合移动
        for (int i = 0; i < Legs.Length; i++) {
            if (ShouldMoveLeg(i)) {
                if (!isMovingLeg[i]) {
                    isMovingLeg[i] = true;
                    movingLegTimer[i] = 0;
                }
            }
        }

        // 移动每个适合移动的脚
        // 不适合移动的脚需要固定在原位
        // 腿采用 IK 控制移动，所以只需要移动脚即可
        for (int i = 0; i < Legs.Length; i++) {
            if (isMovingLeg[i]) {
                movingLegTimer[i] += Time.deltaTime * 2;
                Vector3 pos = Vector3.Lerp(posFeets[i], posHits[i], movingLegTimer[i]);
                pos.y += Mathf.Sin(Mathf.PI * movingLegTimer[i]) * Up;
                Legs[i].position = pos;
                if (movingLegTimer[i] > 1) {
                    isMovingLeg[i] = false;
                    posFeets[i] = posHits[i];
                }
            } else {
                Legs[i].position = posFeets[i];
            }
        }
    }

    private void SimpleMove() {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.Translate(Vector3.forward * Time.deltaTime * 0.5f * v);
        transform.Rotate(Vector3.up, h * 30 * Time.deltaTime);
    }

    /// <summary>
    /// 如果该脚附近两个脚不处于移动状态，
    /// 并且脚的位置距离射线碰撞点位置过远,
    /// 即可移动该脚
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    private bool ShouldMoveLeg(int i) {
        return !isMovingLeg[(i + i % 2 + 1) % Legs.Length]
            && !isMovingLeg[(i + i % 2 + 2) % Legs.Length]
            && Vector3.Distance(posFeets[i], posHits[i]) > FeetMaxDistance;
    }
}