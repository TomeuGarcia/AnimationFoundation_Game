using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Controller : MonoBehaviour
{
    [Header("Strength Slider")]
    [SerializeField] private Slider _strengthSlider;
    private float _strengthSliderSpeed = 2f;
    private int _sliderDirection = 1;


    // Start is called before the first frame update
    void Start()
    {
        _strengthSliderSpeed *= _strengthSlider.maxValue;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ResetStrengthSlider()
    {
        _strengthSlider.value = 0f;
        _sliderDirection = 1;
    }

    public void UpdateStrengthSlider()
    {
        _strengthSlider.value += Time.deltaTime * _strengthSliderSpeed * _sliderDirection;

        if (_strengthSlider.value <= _strengthSlider.minValue) _sliderDirection = 1;
        if (_strengthSlider.value >= _strengthSlider.maxValue) _sliderDirection = -1;
    }

    public float GetStrengthPer1()
    {
        return _strengthSlider.value / _strengthSlider.maxValue;
    }


}
