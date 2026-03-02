using UnityEngine;

public class SimplePlayer : MonoBehaviour // library สำหรับตอน gameplay
{
    private Rigidbody2D rigid; // สำหรับการเคลื่อนที่
    private Animator anim; // สำหรับ animation
    private ParticleSystem grassPar; // particle หญ้า
    private ParticleSystem.EmissionModule emission; // สั่งจำนวนของ particle

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนพื้น
    [SerializeField] private float wallDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนผนัง
    [SerializeField] private LayerMask groundLayer; // หาเฉพาะ layer ของพื้น
    public bool isGrounded = false; // ตรวจชนพื้น
    public bool isWalled = false;  // ตรวจชนกำแพง

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f; // ความเร็วในการเคลื่อนที่แนวราบ
    public float X_input; // ปุ่ม A,D
    public float Y_input; // ปุ่ม s กดให้ slide เร็ว

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f; // แรงกระโดด
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f); // แรงกระโดด wallJump
    public bool isJumping = false;
    public bool isWallJumping = false;
    public bool isWallSliding = false;
    public bool canDoubleJump = false; // doubleJump ได้แค่ครั้งเดียวต่อการกระโดด
    public int facing = 1; // หันหน้าตรงข้ามผนังเวลา wallJumping

    [SerializeField] private float coyoteTimeLimit = .5f; // ระยะเวลาที่สามารถกดเพื่อโดดกลางอากาศได้
    [SerializeField] private float bufferTimeLimit = .5f; // ระยะเวลาที่สามารถกดเพื่อโดดก่อนถึงพื้นได้
    public float coyoteTime = -10000f; // เวลาเริ่มที่ยอมให้กดกระโดดกลางอากาศได้
    public float bufferTime = -10000f; // เวลาเริ่มที่ยอมให้กดกระโดดก่อนถึงพื้นได้

    private void Awake() // ทำงานก่อนเข้ามาใน game
    {
        rigid = GetComponent<Rigidbody2D>(); // มันอยู่ที่ gameobject นี้
        anim = GetComponentInChildren<Animator>(); // ใช้ InChildren เพราะ Animator อยู่ที่ลูก
        grassPar = GetComponentInChildren<ParticleSystem>(); // เอา particle จากลูก
        emission = grassPar.emission; // ดึงข้อมูล emission;
    }
    private void Update() // ทำงานทุก frame
    {
        JumpState(); // ตรวจสถานะว่า อยู่บนพื้น กำลังกระโดด กำลังลงพื้น หรือ wallSlide
        Jump(); // สั่งกระโดดในแบบต่างๆ
        WallSlide(); // สั่ง wallSlide
        InputVal(); // ตรวจ input จากผู้เล่น
        Move(); // สั้งเคลื่อนไหวทั้งบนพื้นและอากาศ
        Flip(); // สั่งหันหน้าไปทางทิศการเคลื่อนที่อัดโนมัต
        GroundAndWallCheck(); // ตรวจจับพื้นและผนัง
        Animation(); // สั่ง animation
    }
    private void JumpState() // ตรวจสถานะตัวละคร
    {
        if (!isGrounded && !isJumping) // fall, takeoff
        {
            isJumping = true; // โดดอยู่

            if (rigid.linearVelocityY <= 0f) // fall หล่นอยู่
            {
                coyoteTime = Time.time; // เริ่มนับเวลา coyote
            }
        }
        if (isGrounded && isJumping) // landing
        {
            isJumping = false;
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }
        if (isWalled) // ถ้าติดกำแพง ตรวจ wallSliding
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;
            if (isGrounded) // ถ้าอยู่บนพื้น
            {
                isWallSliding = false;
            }
            else // ถ้าไม่อยู่บนพื้น
            {
                isWallSliding = true;
            }
        }
        else // ถ้าไม่ติดกำแพง
        {
            isWallSliding = false;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // ถ้ากด spacebar
        {
            if (!isWalled) // ถ้าไม่ติดกำแพง
            {
                if (isGrounded) // ถ้าอยู่บนพื้น
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***normalJump
                }
                else // ถ้าไม่อยู่บนพื้น  doubleJump/ coyoteJump
                {
                    if (rigid.linearVelocityY > 0f && canDoubleJump) // *** DoubleJump
                    {
                        canDoubleJump = false; // doubleJump ซ้ำไม่ได้
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // โดด
                    }

                    if (rigid.linearVelocityY <= 0f)
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit) // *** coyoteJump
                        {
                            coyoteTime = 0f;
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // โดด
                        }
                        else
                        {
                            bufferTime = Time.time; // เริ่มจับ bufferTime
                        }
                    }
                }
            }
            else // ถ้าติดกำแพง แสดงว่าเป็น wallJump
            {
                isWallJumping = true; // จะได้ออกจาก wallSliding
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y); // *** wallJump
            }
        }
        else // ถ้าไม่กด จะเป็น bufferJump                             
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit) // ถ้าอยู่ที่พื้น และอยู่ในเวลาที่ bufferได้
            {
                rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***bufferJump
            }
        }
    }
    private void WallSlide()
    {
        if (!isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return; // ข้ามบรรทัดที่เหลือ

        float Y_slide = Y_input < 0f ? 1f : .5f; // ถ้ากดแป้น s ตกเร็วขึ้น
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide); // ตกช้าลง
    }
    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal"); // GetAxisRaw() ข้อมูลแบบหยาบ
        Y_input = Input.GetAxisRaw("Vertical");
    }
    private void Move()
    {
        if (isWallJumping) // ถ้า wallJumping อยู่ ให้ออกจากการควบคุมจาก player
            return; // มันจะไม่อ่านบรรทัดที่เหลือ

        if (isGrounded) // ถ้าอยู่บนพื้น
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY); // ออกแรงผลัก rigid ให้เคลื่อนที่
        }
        else // ถ้าลอยกลางอากาศ
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX; // แกน X ถ้าไม่กดจะเคลืื่อนที่ตามแรง physics
            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }
    private void Flip() // หมุนตัวละคร ตามทิศการเคลื่อนที่
    {
        if (rigid.linearVelocityX > 0.01f) // ถ้าผลักไปทางขวามือ
        {
            facing = -1; // หันหน้าตรงข้าม
            transform.rotation = Quaternion.identity; // หันตอนเริ่ม
        }
        else if (rigid.linearVelocityX < -0.01f)// ถ้าไม่ผลักทางขวา
        {
            facing = 1; // หันหน้าตรงข้าม
            transform.rotation = Quaternion.Euler(0f, 180f, 0f); // หันไป 180 องศา
        }
    }
    private void GroundAndWallCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer); // sensor พื้น
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer); // sensor ผนัง
    }
    private void OnDrawGizmos() // กราฟฟิกแสดงผลของ sensor ตรวจจับพื้นและผนัง
    {
        Gizmos.color = Color.blue; // เส้นสีน้ำเงิน
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck); // เส้น sensor ตรวจพื้น
        Gizmos.color = Color.red; // เส้นแดง
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck); // เส้น sensor ตรวจผนัง
    }
    private void Animation()
    {
        anim.SetBool("isGrounded", isGrounded); // ตัดสินใจว่า ลอย/อยู่บนพื้น
        anim.SetBool("isWallSliding", isWallSliding); // ตัดสินใจ wallSliding

        /*if(isWalled) // ถ้าติดกำแพงจะหยุดเดิน
        {
            anim.SetFloat("velX", 0f); // หยุด run
        }
        else
        {
            anim.SetFloat("velX", rigid.linearVelocityX); // จะ idle หรือ run
        }*/
        anim.SetFloat("velX", rigid.linearVelocityX); // จะ idle หรือ run
        anim.SetFloat("velY", rigid.linearVelocityY); // จะโดดขึ้นหรือลง

        emission.enabled = isGrounded; // ถ้าอยู่ที่พื้นถึงจะปล่อย
    }
}
