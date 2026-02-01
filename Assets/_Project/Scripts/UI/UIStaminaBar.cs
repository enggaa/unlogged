using UnityEngine;
using UnityEngine.UI;

namespace BrightSouls.UI
{
    public class UIStaminaBar : MonoBehaviour
    {
        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [SerializeField] private ICombatCharacter owner;
        [SerializeField] private Image staminaBar;  // Image의 fillAmount 사용

        /* ------------------------------ Unity Events ------------------------------ */

        private void Start()
        {
            owner.Stamina.onAttributeChanged += OnStaminaChanged;
        }

        /* ----------------------- Attribute Change Callbacks ----------------------- */

        private void OnStaminaChanged(float oldValue, float newValue)
        {
            staminaBar.fillAmount = owner.Stamina.Value / owner.MaxStamina.Value;
        }

        /* -------------------------------------------------------------------------- */
    }
}
