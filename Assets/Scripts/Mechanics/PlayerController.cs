using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.SceneManagement;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        // Variables para el sistema de vidas
        public int maxLives = 3;
        private int currentLives;

        // Tiempo de invulnerabilidad después de recibir daño
        private bool isInvulnerable = false;
        private float invulnerableTime = 1.0f;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            currentLives = maxLives; // Iniciar con 3 vidas
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        // Método para perder vidas
        public void TakeDamage()
        {
            if (!isInvulnerable) // Solo recibe daño si no es invulnerable
            {
                currentLives--;
                Debug.Log("Vidas restantes: " + currentLives);

                if (currentLives <= 0)
                {
                    GameOver();
                }
                else
                {
                    StartCoroutine(InvulnerabilityCooldown());
                }
            }
        }

        // Método de Game Over
        void GameOver()
        {
            Debug.Log("Game Over");
            SceneManager.LoadScene("GameOver"); // Cargar la pantalla de Game Over
        }

        // Activar invulnerabilidad por un tiempo
        IEnumerator InvulnerabilityCooldown()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(invulnerableTime);
            isInvulnerable = false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                float playerBottom = collider2d.bounds.min.y;
                float enemyTop = collision.collider.bounds.max.y;

                // Si el jugador cae sobre el enemigo, lo destruye
                if (playerBottom > enemyTop - 0.1f) // 0.1f es un margen de seguridad
                {
                    Destroy(collision.gameObject); // Eliminar enemigo
                }
                else // Si choca de lado o desde abajo, pierde vida
                {
                    TakeDamage();
                }
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}