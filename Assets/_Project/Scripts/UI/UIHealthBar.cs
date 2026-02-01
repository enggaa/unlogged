using UnityEngine;
using UnityEngine.UI;

namespace BrightSouls.UI
{
    public class UIHealthBar : MonoBehaviour
    {
        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [SerializeField] private ICombatCharacter owner;
        [SerializeField] private Image healthBar;  // Image의 fillAmount 사용

        /* ------------------------------ Unity Events ------------------------------ */

        private void Start()
        {
            owner.Health.onAttributeChanged += OnHealthChanged;
        }

        /* ----------------------- Attribute Change Callbacks ----------------------- */

        private void OnHealthChanged(float oldValue, float newValue)
        {
            healthBar.fillAmount = owner.Health.Value / owner.MaxHealth.Value;
        }

        /* -------------------------------------------------------------------------- */
    }
}
