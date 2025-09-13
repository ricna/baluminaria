public class Baluminaria : MonoBehaviour
{
    [SerializeField]
    private Segment[] _prefabSegments; // Array to hold segment our 7 prefabs

    // Armazena todos os segmentos instanciados para fácil acesso
    private Segment[] _allSegments = new Segment[84]; 
    public Segment[] GetSegments() => _allSegments;


    private void Awake() // Alterado de Start para Awake para garantir que esteja pronto antes do Start de outros scripts
    {
        Initialize();
    }

    private void Initialize()
    {
        // Lógica de instanciamento ajustada para garantir que tenhamos exatamente 84 segmentos
        // Esta lógica deve ser adaptada à sua geometria real.
        // O exemplo abaixo assume que `_prefabSegments` contém os prefabs corretos em ordem.
        // Se a sua inicialização for diferente, o importante é popular o array `_allSegments` com 84 segmentos na ordem correta.
        
        var existingSegments = GetComponentsInChildren<Segment>();
        if (existingSegments.Length >= 84)
        {
            // Assume que os segmentos já existem e estão em ordem correta no hierarchy
            for (int i = 0; i < 84; i++)
            {
