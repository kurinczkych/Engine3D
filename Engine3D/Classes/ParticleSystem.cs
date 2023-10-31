using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Particle
    {
        public InstancedMeshData meshData;

        private float lifetime;
        private float maxLifetime;
        private Vector3 position;
        private Vector3 velocity;

        private ParticleSystem emitter;

        public Particle(Vector3 startPos, ref ParticleSystem emitter_)
        {
            position = startPos;
            emitter = emitter_;

            meshData = new InstancedMeshData();
            meshData.Position = startPos;
            meshData.Rotation = emitter.startRotation;
            meshData.Scale = emitter.startScale;
            meshData.Color = emitter.startColor;

            lifetime = emitter.lifetime;
            maxLifetime = emitter.lifetime;
        }

        public bool Update(float delta)
        {
            float normLifetime = lifetime/maxLifetime;

            lifetime -= delta;
            if (lifetime <= 0)
                return true;

            position += velocity * delta;
            meshData.Position = position;
            meshData.Rotation = Quaternion.Slerp(emitter.startRotation, emitter.endRotation, normLifetime);
            meshData.Scale = Vector3.Lerp(emitter.startScale, emitter.endScale, normLifetime);
            meshData.Color = Helper.LerpColor(emitter.startColor, emitter.endColor, normLifetime);


            return false;
        }
    }

    public class ParticleSystem
    {
        public float duration = 5;
        public bool looping = true;
        public bool useGravity = false;

        public float startDelay = 0;

        public float emitTimeSec = 0.2f;
        private float time;

        public float lifetime = 5;
        public bool randomLifeTime = false;
        public float xLifeTime = 5;
        public float yLifeTime = 10;

        public Vector3 startDir = Vector3.UnitY;

        public float startSpeed = 5;
        public float endSpeed = 5;
        public bool randomSpeed = false;
        public float xStartSpeed = 5;
        public float yStartSpeed = 10;
        public float xEndSpeed = 5;
        public float yEndSpeed = 10;

        public Vector3 startScale = Vector3.One;
        public Vector3 endScale = Vector3.One;
        public bool randomScale = false;
        public AABB xStartScale = new AABB();
        public AABB xEndScale = new AABB();

        public Quaternion startRotation = Quaternion.Identity;
        public Quaternion endRotation = Quaternion.Identity;
        public bool randomRotation = false;

        public Color4 startColor = Color4.White;
        public Color4 endColor = Color4.White;
        public bool randomColor = false;

        private Object meshObject;

        private List<Particle> particles = new List<Particle>();

        public ParticleSystem(Object meshObject)
        {
            if (meshObject.GetMesh().GetType() != typeof(InstancedMesh))
                throw new Exception("The particle system's object mesh only can be InstancedMesh");

            this.meshObject = meshObject;
        }

        public void Update(float delta)
        {
            time += delta;
            if(time >= emitTimeSec)
            {
                time = 0;
                //EMIT
            }

            for (int i = particles.Count; i >= 0; i--)
            {
                if (particles[i].Update(delta))
                {
                    particles.RemoveAt(i);
                }
            }
        }

        private void AddParticle()
        {
            Particle = new Particle();
        }

        public Object GetObject()
        { 

        }

    }
}
