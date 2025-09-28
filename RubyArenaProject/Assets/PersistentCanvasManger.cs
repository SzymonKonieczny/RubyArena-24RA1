using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PersistentCanvasManger : MonoBehaviour
{

    [SerializeField] Slider Healthbar;
    [SerializeField] Image Crosshair;
    [SerializeField] public CharacterSelectUIScript CharacterSelect;
    [SerializeField]
    private PlayerScript _playerScript;
    public PlayerScript playerScript //initialized by the playerscript on spawn
    {
        get { return _playerScript; }
        set
        {
            _playerScript = value;
        }
    }


    public static PersistentCanvasManger Instance;
    // Start is called before the first frame update
    private void Start()
    {
        if (Instance != this)
            Instance = this;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
