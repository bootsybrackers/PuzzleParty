using UnityEngine;

namespace PuzzleParty.Board.Effects
{
    public class IceBreakEffect : MonoBehaviour
    {
        private ParticleSystem particles;

        private void Awake()
        {
            particles = GetComponent<ParticleSystem>();
        }

        public void Play(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            particles.Play();
            Destroy(gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
    }
}
