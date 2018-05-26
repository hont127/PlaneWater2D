using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hont
{
    public class PlaneWater2D_EffectPoint : MonoBehaviour
    {
        public Transform Point;
        public float TakeForce = 0.3f;

        public bool InteractFlag { get; set; }
    }
}
