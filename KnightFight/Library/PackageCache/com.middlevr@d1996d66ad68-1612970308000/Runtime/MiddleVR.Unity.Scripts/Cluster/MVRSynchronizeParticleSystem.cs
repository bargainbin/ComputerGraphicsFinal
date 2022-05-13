/* MVRSynchronizeParticleSystem
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR.Unity;
using UnityEngine;

// Synchronize the random seed of ParticleSystem and start it
// at the same time on all nodes
[RequireComponent(typeof(ParticleSystem))]
public class MVRSynchronizeParticleSystem : MonoBehaviour
{
    private struct ParticleSystemData
    {
        public uint RandomSeed;
        public bool PlayOnAwake;
    }

    #region MonoBehaviour
    private void Awake()
    {
        var particleSystem = GetComponent<ParticleSystem>();

        Cluster.AddMessageHandler(this,
            (ParticleSystemData data) =>
            {
                particleSystem.randomSeed = data.RandomSeed;

                if (data.PlayOnAwake)
                {
                    particleSystem.Play();
                }

                Cluster.Remove(this);
            },
            addCleanBehaviour: false);

        Cluster.BroadcastMessage(this,
            new ParticleSystemData
            {
                RandomSeed = particleSystem.randomSeed,
                PlayOnAwake = particleSystem.main.playOnAwake
            });

        particleSystem.Clear();
        particleSystem.Stop();
        particleSystem.time = .0f;
    }
    #endregion
}
