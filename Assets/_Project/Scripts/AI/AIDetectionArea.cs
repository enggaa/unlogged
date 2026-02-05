using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightSouls.AI
{

    [RequireComponent(typeof(Collider))]
    public class AIDetectionArea : MonoBehaviour
    {

        private Collider coll;
        private AICharacter owner;

        public LayerMask sightRaycastLayer;

        private void Start()
        {
            owner = GetComponentInParent<AICharacter>();
            if (owner == null)
            {
                Debug.LogWarning($"{nameof(AIDetectionArea)} on \"{name}\" could not find an AICharacter owner.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null || other == null)
            {
                return;
            }

            Vector3 dir = (other.transform.position - transform.position).normalized;
            Ray r = new Ray(transform.position, dir);
            ICombatCharacter otherCharacter = other.GetComponent<ICombatCharacter>();
            if (otherCharacter != null)
            {
                owner.Target = otherCharacter;
            }
        }

        private void OnTriggerExit(Collider other)
        {

        }

    }
}