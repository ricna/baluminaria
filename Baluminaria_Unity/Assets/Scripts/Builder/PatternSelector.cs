using UnityEngine;

namespace BaluminariaBuilder
{
    [RequireComponent(typeof(Collider))]
    public class PatternSelector : MonoBehaviour
    {
        [Tooltip("O padrão que este objeto vai aplicar ao ser clicado.")]
        public ColorPattern patternType;
    }
}