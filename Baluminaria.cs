public class Baluminaria : MonoBehaviour
{
    [SerializeField]
    private Segment[] _prefabSegments; // Array to hold segment our 7 prefabs

    // Armazena todos os segmentos instanciados para f�cil acesso
    private Segment[] _allSegments = new Segment[84]; 
    public Segment[] GetSegments() => _allSegments;


    private void Awake() // Alterado de Start para Awake para garantir que esteja pronto antes do Start de outros scripts
    {
        Initialize();
    }

    private void Initialize()
    {
        // L�gica de instanciamento ajustada para garantir que tenhamos exatamente 84 segmentos
        // Esta l�gica deve ser adaptada � sua geometria real.
        // O exemplo abaixo assume que `_prefabSegments` cont�m os prefabs corretos em ordem.
        // Se a sua inicializa��o for diferente, o importante � popular o array `_allSegments` com 84 segmentos na ordem correta.
        
        var existingSegments = GetComponentsInChildren<Segment>();
        if (existingSegments.Length >= 84)
        {
            // Assume que os segmentos j� existem e est�o em ordem correta no hierarchy
            for (int i = 0; i < 84; i++)
            {
