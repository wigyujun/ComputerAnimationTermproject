using System.Collections;
using UnityEngine;

public class WeaponVisualController : MonoBehaviour
{
    [System.Serializable]
    public class WeaponVisualData
    {
        public Sprite idleSprite;
        public Sprite fireSprite;

        public Vector3 localPosition;
        public Vector3 firePointLocalPosition;

        public float rotationZ;
    }

    [Header("References")]
    [SerializeField] private PlayerCombatStats combatStats;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Transform firePoint;

    [Header("Weapon Visuals")]
    [SerializeField] private WeaponVisualData bow;
    [SerializeField] private WeaponVisualData pistol;
    [SerializeField] private WeaponVisualData rifle;
    [SerializeField] private WeaponVisualData shotgun;
    [SerializeField] private WeaponVisualData laser;

    [Header("Shoot Feedback")]
    [SerializeField] private float firePoseDuration = 0.06f;
    [SerializeField] private Vector3 recoilOffset = new Vector3(0f, -0.03f, 0f);

    private WeaponType currentWeaponType;
    private Coroutine fireRoutine;

    private void Start()
    {
        RefreshVisual(true);
    }

    private void Update()
    {
        RefreshVisual(false);
    }

    public void RefreshVisual(bool force)
    {
        if (combatStats == null || weaponRenderer == null)
            return;

        WeaponType nextType = combatStats.CurrentWeaponType;

        if (!force && nextType == currentWeaponType)
            return;

        currentWeaponType = nextType;
        ApplyWeaponData(GetCurrentData(), false);
    }

    public void PlayShootMotion()
    {
        if (weaponRenderer == null)
            return;

        if (fireRoutine != null)
            StopCoroutine(fireRoutine);

        fireRoutine = StartCoroutine(CoPlayShootMotion());
    }

    private IEnumerator CoPlayShootMotion()
    {
        WeaponVisualData data = GetCurrentData();

        if (data.fireSprite != null)
            weaponRenderer.sprite = data.fireSprite;
        else
            weaponRenderer.sprite = data.idleSprite;

        transform.localPosition = data.localPosition + recoilOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, data.rotationZ);

        yield return new WaitForSeconds(firePoseDuration);

        ApplyWeaponData(data, false);
        fireRoutine = null;
    }

    private void ApplyWeaponData(WeaponVisualData data, bool useFireSprite)
    {
        if (data == null)
            return;

        if (useFireSprite && data.fireSprite != null)
            weaponRenderer.sprite = data.fireSprite;
        else
            weaponRenderer.sprite = data.idleSprite;

        transform.localPosition = data.localPosition;
        transform.localRotation = Quaternion.Euler(0f, 0f, data.rotationZ);

        if (firePoint != null)
            firePoint.localPosition = data.firePointLocalPosition;
    }

    private WeaponVisualData GetCurrentData()
    {
        switch (combatStats.CurrentWeaponType)
        {
            case WeaponType.Bow: return bow;
            case WeaponType.Pistol: return pistol;
            case WeaponType.Rifle: return rifle;
            case WeaponType.Shotgun: return shotgun;
            case WeaponType.Laser: return laser;
            default: return bow;
        }
    }

    public Transform GetFirePoint()
    {
        return firePoint;
    }
}
