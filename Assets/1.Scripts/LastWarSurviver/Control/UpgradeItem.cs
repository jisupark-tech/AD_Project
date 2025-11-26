using System.Collections;
using UnityEngine;
using TMPro;

public class UpgradeItem : MonoBehaviour
{
    [Header("UI")]
    public TextMeshPro TxtHpVal;

    [Header("Setting")]
    public int HP=20;

    [Header("Sprite")]
    public Sprite[] m_Box = new Sprite[2];
    public SpriteRenderer body;

    private bool IsDead = false;
    public void SetItemSetting(int _hp)
    {
        HP = _hp;
        body.sprite = m_Box[0];
        TxtHpVal.text = HP.ToString();
    }
    private void Damage()
    {
        HP--;
        TxtHpVal.text = HP.ToString();
        GameObject _effect = ObjectPool.Instance.GetPooledObject("Effect_1");
        if (_effect != null)
        {
            _effect.transform.position = this.transform.position;
            _effect.SetActive(true);
        }

        if (HP <= 0)
        {
            body.sprite = m_Box[1];
            GameManager.Instance.m_Player.UPdatePlayerCharacter();

            IsDead = true;
            StartCoroutine(hide());
        }
    }
    IEnumerator hide()
    {
        yield return new WaitForSeconds(0.5f);
        this.gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            if (!IsDead)
                Damage();
        }
    }
    
}
