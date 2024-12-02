using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class AccMovingPlatform : MovingPlatform
    {
        [SerializeField] protected float speedRate = 0.5f;
        [SerializeField] protected float accPow = 4;
        [SerializeField] protected float returnSpeedRate = 0.2f;
        [SerializeField] protected float accReleaseBonusTime = 0.4f;
        [SerializeField] protected float accMaxLength = 18;
        [SerializeField] protected float accMaxTime = 1;
        public override void Move()
        {
            base.Move();
            if (!isWorking) return;
            if (isGoDir)
            {
                GoStartTime += Time.deltaTime;
                // IsStartPoint = false;
                var dir = (TargetPosition - StartPosition).normalized;
                // Velocity = dir  * Time.deltaTime;
                Velocity = dir * (Length * speedRate * Mathf.Pow(GoStartTime, accPow) * Time.deltaTime);
                
                var farDir = TargetPosition - transform.position;
                var nearDir = Velocity;
                // Debug.Log(farDir);
                // Debug.Log(nearDir);
                if (farDir.magnitude < nearDir.magnitude)
                {
                    Velocity = TargetPosition - transform.position;
                    
                    transform.position += Velocity; 
                    isGoDir = false;
                    
                    GoEndTime = 0;
                    
                    finishSoundSource.Play();
                }
                else transform.position += Velocity;

                if (Velocity.magnitude < 0.05f)
                {
                    Acc = Velocity;
                }
                else
                {
                    Acc = Velocity.normalized * (accMaxLength / accMaxTime);
                }
                
            }
            else
            {
                GoEndTime += Time.deltaTime;
                Velocity = Vector3.zero;
                if (GoEndTime > accReleaseBonusTime)
                {
                    Acc = Vector3.zero;
                    var dir = (StartPosition - TargetPosition).normalized;
                    Velocity = dir * (Length * returnSpeedRate * Time.deltaTime);
                    // Velocity = dir  * Time.deltaTime;

                    var farDir = StartPosition - transform.position;
                    var nearDir = Velocity;
                    if (farDir.magnitude < nearDir.magnitude)
                    {
                        Velocity = StartPosition - transform.position;
                        transform.position += Velocity;
                        isGoDir = true;
                        GoStartTime = 0;
                        // IsStartPoint = true;
                        if (!IsPlayerOnPlatform) isWorking = false;
                    }
                    else transform.position += Velocity;
                }
            }
        }
    }
}