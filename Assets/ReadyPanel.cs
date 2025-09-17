using System;
using UnityEngine;
using UnityEngine.UI;

public class ReadyPanel : MonoBehaviour
{
    public GameManager gameManager;
    public Slider battleSize;
    public Text battleSizeText;
    public Slider battleRateCount;
    public Text battleRateBallCountText;
    public Slider InitialHP;
    public Text InitialHPText;
    public Slider areaPowerRate;
    public Text areaPowerRateText;
    private void Awake()
    {
        battleSize.onValueChanged.AddListener(OnBattleSizeChanged);
        OnBattleSizeChanged(battleSize.value);
        battleRateCount.onValueChanged.AddListener(OnBattleRateBallCountChanged);
        OnBattleRateBallCountChanged(battleRateCount.value);
        InitialHP.onValueChanged.AddListener(OnInitialHPChanged);
        OnInitialHPChanged(InitialHP.value);
        areaPowerRate.onValueChanged.AddListener(OnAreaPowerRateChanged);
        OnAreaPowerRateChanged(areaPowerRate.value);
    }

    private void OnAreaPowerRateChanged(float rate)
    {
        gameManager.areaPowerRate = rate;
        areaPowerRateText.text = $"{rate:F2}";
    }

    private void OnInitialHPChanged(float hp)
    {
        hp = hp * hp * 100_000_000 + 10;
        gameManager.initialHP = (long)hp;
        InitialHPText.text = $"{Ball.FormatNumber((long)hp)}";
    }

    private void OnBattleRateBallCountChanged(float count)
    {
        gameManager.rateBallCount = (int)count;
        battleRateBallCountText.text = $"{(int)count}";
    }

    private void OnBattleSizeChanged(float size)
    {
        gameManager.battleSize = (int)size;
        battleSizeText.text = $"{(int)size} x {(int)size}";
    }
    public void OnStartClick()
    {
        gameManager.StartNewBattle();
        gameObject.SetActive(false);
    }
}
