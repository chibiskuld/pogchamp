using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace MilkCaps
{
    public class Slammer : UdonSharpBehaviour
    {
        [SerializeField] public VRC_Pickup pickup;
        [SerializeField] public VRCObjectSync objectSync;
        [SerializeField] private AudioSource slamSound;
        [SerializeField] private Transform anchor;
        public void OnCollisionEnter(Collision collision)
        {
            if (!pickup.IsHeld)
            {
                if (collision.contacts[0].otherCollider.gameObject.layer == 0)
                {
                    slamSound.Play();
                }
            }
        }

        public void DoRespawn()
        {
            pickup.Drop();
            
            if (!Networking.IsOwner(gameObject)) return;
            objectSync.Respawn(); 
            //sometimes fails, so also:
            transform.position = anchor.position;
            transform.rotation = anchor.rotation;
        }
    }
}