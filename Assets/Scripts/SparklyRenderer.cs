using System;
using Unity.Burst;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

//�򵥵�CPU����ϵͳ
[ExecuteAlways]
public class SparklyRenderer : MonoBehaviour
{
    //��Ⱦʹ�õ���Դ
    public Sprite sprite;
    public Material material;
    public Mesh mesh;

    //�����������
    static Unity.Mathematics.Random random = new Unity.Mathematics.Random(20090505);

    [Header("Properties")]

    //������A
    public int capacityA = 150; //������A������������ӵĿռ��С
    public SparklyEmitter emitterA = new()
    {
        spawnRate = new(150f, 150f),
        box = new(0f, 290f, 0f, 135f, Curve.FastInOutWeak),
        skew = new(-0.3f, -0.3f, 0f, 0f),
        duration = new(0.2f, 0.8f),

        velocity = new(0f, -10f, -2f, 2f),

        alpha = 0.8f,

        time0 = 0.0f, 
        scale0 = new(0.3f, 0.6f),
        time1 = 0.1f,
        scale1 = new(0.4f, 0.8f),
        time2 = 0.5f,
        scale2 = new(0.5f, 1.0f),
        time3 = 0.9f,
        scale3 = new(0.4f, 0.8f),
        time4 = 1.0f,
        scale4 = new(0.3f, 0.6f)
    };
    //������B
    public int capacityB = 20; //������B������������ӵĿռ��С
    public SparklyEmitter emitterB = new()
    {
        spawnRate = new(20f, 20f),
        box = new(-330f, 260f, 0f, 135f),
        skew = new(0f, 0f, 0f, 0f),
        duration = new(0.2f, 0.6f),

        velocity = new(0f, -5f, -2f, 2f),

        alpha = 0.8f,

        time0 = 0.0f,
        scale0 = new(0.3f, 0.6f),
        time1 = 0.2f,
        scale1 = new(0.4f, 0.8f),
        time2 = 0.5f,
        scale2 = new(0.5f, 1.0f),
        time3 = 0.8f,
        scale3 = new(0.4f, 0.8f),
        time4 = 1.0f,
        scale4 = new(0.3f, 0.6f)
    };

    //���´�����Ⱦʹ�õ�ģ��
    public void CreateMesh()
    {
        if (mesh)
        {
            DestroyImmediate(mesh);
        }
        mesh = new Mesh();
        mesh.SetVertices(Array.ConvertAll(sprite.vertices, (Vector2 vec2) => (Vector3)vec2));
        mesh.SetIndices(sprite.triangles, MeshTopology.Triangles, 0);
        mesh.uv = sprite.uv;
        mesh.name = sprite.name;
    }

    public void Awake()
    {
#if UNITY_EDITOR
        runInEditMode = true;
#endif
        CreateMesh();

        emitterA.pixelsToUnit = 1f / sprite.pixelsPerUnit;
        emitterB.pixelsToUnit = 1f / sprite.pixelsPerUnit;
        emitterA.Initialize(capacityA);
        emitterB.Initialize(capacityB);
#if UNITY_EDITOR
        if (forceUpdateInEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    public void OnValidate()
    {
        Awake();
    }

    public void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
        emitterA.Dispose(); 
        emitterB.Dispose();
    }

    //ÿ��һ�ٴ�
    public void FixedUpdate()
    {
        //��������
        emitterA.localToWorld = transform.localToWorldMatrix;
        emitterB.localToWorld = transform.localToWorldMatrix;
        emitterA.UpdateEmitter();
        emitterB.UpdateEmitter();
    }

    //ÿ֡һ��
    [ExecuteAlways]
    public void Update()
    {
        //��Ⱦ
        DrawEmitter(ref emitterA);
        DrawEmitter(ref emitterB);
    }
    //��Ⱦ������
    public void DrawEmitter(ref SparklyEmitter emitter)
    {
        if (!mesh || !material || emitter.matrices == null || emitter.matrices.Length < 1) return;
        var color = material.color;
        color.a = emitter.alpha;
        material.color = color;
        //Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
        var renderParams = new RenderParams(material);
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        Graphics.RenderMeshInstanced(renderParams, mesh, 0, emitter.matrices);
    }

#if UNITY_EDITOR
    //�ڱ༭�����Թ̶���������
    public void EditorUpdate()
    {
        //���²���Ⱦ����
        if (forceUpdateInEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            FixedUpdate();
            EditorUtility.SetDirty(this);
        }
    }
    //ǿ���ڱ༭�����²���Ⱦ����
    public bool forceUpdateInEditMode;
    //������Ϣ����
    public void OnDrawGizmosSelected()
    {
        //�����������ķ�Χ
        var center0 = (Vector3)emitterA.box.GetCenter() / sprite.pixelsPerUnit;
        center0.y = -center0.y;
        center0 += transform.position;
        var center1 = (Vector3)emitterB.box.GetCenter() / sprite.pixelsPerUnit;
        center1.y = -center1.y;
        center1 += transform.position;
        var size0 = emitterA.box.GetSize() / sprite.pixelsPerUnit * transform.lossyScale;
        var size1 = emitterB.box.GetSize() / sprite.pixelsPerUnit * transform.lossyScale;

        //�ɵ���ɫ
        var color = Gizmos.color;

        //���������������ĵ�����Ϣ
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center0, size0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center1, size1);

        //������ɫ
        Gizmos.color = color;
    }
#endif

    //����
    [Serializable]
    public struct SparklyInstance
    {
        public bool alive;    //�����Ƿ���
        public float timestart;//���ӿ�ʼʱ��
        public float timeend;  //���ӽ���ʱ��
        public Vector2 position;//���ӵ�ǰλ��
        public Vector2 velocity;//���ӵ��ٶ�
        public float scale;    //���ӵ�ǰ������
        public float scale0;   //��0֡������
        public float scale1;   //��1֡������
        public float scale2;   //��2֡������
        public float scale3;   //��3֡������
        public float scale4;   //��4֡������
    };
    //������
    [Serializable]
    public unsafe struct SparklyEmitter : IDisposable
    {
        //�ű����ڵ�GameObject�ľ���
        [HideInInspector]
        public Matrix4x4 localToWorld;

        //��ʼ��������
        public void Initialize(int capacity)
        {
            spawnRateValue = spawnRate.Random();
            //��ʼ����������
            particles = new NativeArray<SparklyInstance>(capacity, Allocator.Persistent);
            deadParticles = new NativeList<int>(capacity, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(capacity, Allocator.Persistent);

            //һ��ʼû������
            for (int i = capacity - 1; i >= 0; i--)
            {
                deadParticles.Add(i);
            }
        }
        //���ڸ�������, ��FixedUpdate��ִ��, Ҳ������Update������
        public void UpdateEmitter(float deltaTime = -1f)
        {
            if (deltaTime < 0f)
            {
                deltaTime = Time.fixedDeltaTime;
            }
            //��������
            spawnAccumulate += spawnRateValue * deltaTime;
            var spawnCount = (int)spawnAccumulate;
            spawnAccumulate -= spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                Spawn();
            }

            //��������
            var job = new UpdateJob
            {
                deltaTime = deltaTime,
                pixelsToUnit = pixelsToUnit,
                localToWorld = localToWorld,
                particles = particles,
                deadParticles = deadParticles,
                matrices = matrices,
                time = Time.time,
                time0 = time0,
                time1 = time1,
                time2 = time2,
                time3 = time3,
                time4 = time4
            };
            var jobHandle = job.Schedule(particles.Length, new JobHandle());
            jobHandle.Complete();
        }
        //�������ݵ���ҵ
        [BurstCompile]
        public struct UpdateJob : IJobFor
        {
            public float deltaTime;
            public float pixelsToUnit;
            public Matrix4x4 localToWorld;

            public NativeArray<SparklyInstance> particles;
            public NativeList<int> deadParticles;
            public NativeArray<Matrix4x4> matrices;

            public float time;
            public float time0;
            public float time1;
            public float time2;
            public float time3;
            public float time4;

            [BurstCompile]
            public void Execute(int index)
            {
                var particle = particles[index];
                //��������Ѿ�����������
                if (!particle.alive) return;
                //���ʱ�䵽�˾�ɱ������
                if (time > particle.timeend)
                {
                    //��������Ѿ�����
                    if (!particles[index].alive)
                    {
                        return;
                    }
                    //ɱ������
                    particle.alive = false;
                    particles[index] = particle;
                    deadParticles.Add(index);
                    matrices[index] = Matrix4x4.zero; //��������ʧ
                    return;
                }
                //������Ⱦʹ�õľ���
                particle.position += particle.velocity * deltaTime;
                //��������
                var age = (time - particle.timestart) / (particle.timeend - particle.timestart);
                switch (age)
                {
                    case float t when t <= time0: particle.scale = particle.scale0; break;
                    case float t when t <= time1: particle.scale = math.lerp(particle.scale0, particle.scale1, (age - time0) / (time1 - time0)); break;
                    case float t when t <= time2: particle.scale = math.lerp(particle.scale1, particle.scale2, (age - time1) / (time2 - time1)); break;
                    case float t when t <= time3: particle.scale = math.lerp(particle.scale2, particle.scale3, (age - time2) / (time3 - time2)); break;
                    case float t when t <= time4: particle.scale = math.lerp(particle.scale3, particle.scale4, (age - time3) / (time4 - time3)); break;
                    case float t when t > time4: particle.scale = particle.scale4; break;
                    default: particle.scale = particle.scale0; break;
                }

                var position = particle.position;
                position.y = -position.y;
                position *= pixelsToUnit;
                var matrix = localToWorld * Matrix4x4.TRS(position, Quaternion.identity, particle.scale * Vector3.one);
                matrices[index] = matrix;
                particles[index] = particle;
            }
        }

        //��������
        public int Spawn()
        {
            //����޷������µ�����
            if (deadParticles.Length < 1)
            {
                return -1;
            }
            //�����µ�����
            var index = deadParticles[^1];
            deadParticles.RemoveAt(deadParticles.Length - 1);
            var particle = particles[index];
            particle.alive = true;
            //���ó�ʼ����
            particle.timestart = Time.time;
            particle.timeend = particle.timestart + duration.Random();
            particle.position = box.Random();
            var skewValue = skew.Random();
            particle.position.x += particle.position.y * skewValue.x;
            particle.position.y += particle.position.x * skewValue.y;
            particle.velocity = velocity.Random();
            particle.scale0 = scale0.Random();
            particle.scale1 = scale1.Random();
            particle.scale2 = scale2.Random();
            particle.scale3 = scale3.Random();
            particle.scale4 = scale4.Random();
            particles[index] = particle;
            //��������
            return index;
        }
        //ɱ������
        public void Kill(int index)
        {
            //��������Ѿ�����
            if (!particles[index].alive)
            {
                return;
            }
            //ɱ������
            var particle = particles[index];
            particle.alive = false;
            particles[index] = particle;
            deadParticles.Add(index);
            matrices[index] = Matrix4x4.zero; //��������ʧ
        }

        //��ȡ��������
        public float GetParticleScale(ref SparklyInstance particle)
        {
            var time = (Time.time - particle.timestart) / (particle.timeend - particle.timestart);
            switch (time)
            {
                case float t when t <= time0:
                    return particle.scale0;
                case float t when t <= time1:
                    return math.lerp(particle.scale0, particle.scale1, (time - time0) / (time1 - time0));
                case float t when t <= time2:
                    return math.lerp(particle.scale1, particle.scale2, (time - time1) / (time2 - time1));
                case float t when t <= time3:
                    return math.lerp(particle.scale2, particle.scale3, (time - time2) / (time3 - time2));
                case float t when t <= time4:
                    return math.lerp(particle.scale3, particle.scale4, (time - time3) / (time4 - time3));
                case float t when t > time4:
                    return particle.scale4;
                default: return particle.scale0;
            }
        }

        //�ͷŷ��й���Դ
        public void Dispose()
        {
            particles.Dispose();
            deadParticles.Dispose();
        }

        //�������ӵ�����
        public NativeArray<SparklyInstance> particles;
        //���ӵľ���
        public NativeArray<Matrix4x4> matrices;

        //�������ӵ�����
        public NativeList<int> deadParticles;
        public float pixelsToUnit;

        //����ϵͳ������
        public float spawnRateValue;
        public float spawnAccumulate;
        public Range1 spawnRate;
        public Range2 box;
        public Range2 skew;

        //���ӵ�����
        public Range1 duration;
        public Range2 velocity;
        public float alpha;

        //���ŵ�5���ؼ�֡
        public float time0;
        public Range1 scale0;
        public float time1;
        public Range1 scale1;
        public float time2;
        public Range1 scale2;
        public float time3;
        public Range1 scale3;
        public float time4;
        public Range1 scale4;
    }
    
    //��ʾ���η�Χ
    [Serializable]
    public struct Range2
    {
        public Range2(
            float startX, float endX, 
            float startY, float endY, 
            Curve distributionX = Curve.Linear, 
            Curve distributionY = Curve.Linear)
        {
            x = new(startX, endX, distributionX);
            y = new(startY, endY, distributionY);
        }
        public Vector2 Random() => new Vector2(x.Random(), y.Random());
        public Vector2 GetSize() => new Vector2(x.GetSize(), y.GetSize());
        public Vector2 GetCenter() => new Vector2(x.GetCenter(), y.GetCenter());

        public Range1 x; //x��Χ
        public Range1 y; //y��Χ
    }
    //��ʾ�������ķ�Χ
    [Serializable]
    public struct Range1
    {
        public Range1(float start, float end, Curve distribution = Curve.Linear)
        {
            this.start = start;
            this.end = end;
            this.distribution = distribution;
        }

        //�ڹ涨��Χ��ʹ���������һ��ֵ
        public float Random() => Evaluate(start, end, random.NextFloat(), distribution);
        //��ȡ��Χ�Ĵ�С
        public float GetSize() => math.abs(end - start);
        //��ȡ��Χ������
        public float GetCenter() => (start + end) * 0.5f;
        public float start; //��ʼֵ
        public float end; //����ֵ
        public Curve distribution; //����
    }

    //ʹ�����߲�ֵ
    public static float Evaluate(float start, float end, float time, Curve curve)
    {
        return curve switch
        {
            Curve.Linear => math.lerp(start, end, time),
            Curve.FastInOutWeak => math.lerp(start, end, FastInOutWeak(time)),
            _ => math.lerp(start, end, time),
        };
    }
    public static float FastInOutWeak(float time)
    {
        //�������κ���

        //a=-1,b=2,c=0
        float In(float t)
        {
            return 2 * t - t * t;
        }
        //a=1,b=0,c=0
        float Out(float t)
        {
            return t * t;
        }


        //��ǰ�벿��ʹ�õ�һ��
        if (time <= 0.5f)
        {
            return In(time * 2.0f) * 0.5f;
        }
        //�ں�벿��ʹ�õڶ���
        return Out((time - 0.5f) * 2.0f) * 0.5f + 0.5f;
    }
    //��ֵʹ�õ�����
    [Serializable]
    public enum Curve
    {
        Linear,
        FastInOutWeak
    }
}