using System;
using _Project.Characters.IngameCharacters.Core;
using _Project.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class Portal : MonoBehaviour
    { 
        public enum Type
        {
            Depart,
            Arrival,
        }
        
        [SerializeField] private string id;

        public string ID
        {
            get => id;
            set => id = value;
        }
        
        [SerializeField] private Type type;

        public Type PortalType
        {
            get => type;
            set => type = value;
        }

        [SerializeField] private string arrivalID;

        public string ArrivalID
        {
            get => arrivalID;
            set => arrivalID = value;
        }

        [SerializeField] private Portal arrivalPortal;

        public Portal ArrivalPortal
        {
            get => arrivalPortal;
            set => arrivalPortal = value;
        }

        [SerializeField] private SavePoint targetSavePoint;

        public SavePoint TargetSavePoint
        {
            get => targetSavePoint;
            set => targetSavePoint = value;
        }

        private LayerMask targetLayers;
        
        private void Awake()
        {
            targetLayers = 1 << LayerMask.NameToLayer("Player");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (type == Type.Arrival) return;
            if (other.gameObject.layer.IsInLayerMask(targetLayers))
            {
                var character = other.GetComponent<IngameCharacter>();
                if (character.IsDying || character.IsDead) return;
                character.SavePoint = arrivalPortal.TargetSavePoint;
                character.MoveToSavePoint();
            }
        }
    }
}