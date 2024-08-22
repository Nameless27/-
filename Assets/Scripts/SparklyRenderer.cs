using System;
using Unity.Burst;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

//简单的CPU粒子系统
[ExecuteAlways]
public class SparklyRenderer : MonoBehaviour
{
    //渲染使用的资源
    public Sprite sprite;
    public Material material;
    public Mesh mesh;

    //随机数生成器
    static Unity.Mathematics.Random random = new Unity.Mathematics.Random(20090505);

    [Header("Properties")]

    //发射器A
    public int capacityA = 150; //发射器A分配给所有粒子的空间大小
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
    //发射器B
    public int capacityB = 20; //发射器B分配给所有粒子的空间大小
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

    //重新创建渲染使用的模型
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

    //每秒一百次
    public void FixedUpdate()
    {
        //更新数据
        emitterA.localToWorld = transform.localToWorldMatrix;
        emitterB.localToWorld = transform.localToWorldMatrix;
        emitterA.UpdateEmitter();
        emitterB.UpdateEmitter();
    }

    //每帧一次
    [ExecuteAlways]
    public void Update()
    {
        //渲染
        DrawEmitter(ref emitterA);
        DrawEmitter(ref emitterB);
    }
    //渲染发射器
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
    //在编辑器中以固定速率运行
    public void EditorUpdate()
    {
        //更新并渲染粒子
        if (forceUpdateInEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            FixedUpdate();
            EditorUtility.SetDirty(this);
        }
    }
    //强制在编辑器更新并渲染粒子
    public bool forceUpdateInEditMode;
    //调试信息绘制
    public void OnDrawGizmosSelected()
    {
        //两个发射器的范围
        var center0 = (Vector3)emitterA.box.GetCenter() / sprite.pixelsPerUnit;
        center0.y = -center0.y;
        center0 += transform.position;
        var center1 = (Vector3)emitterB.box.GetCenter() / sprite.pixelsPerUnit;
        center1.y = -center1.y;
        center1 += transform.position;
        var size0 = emitterA.box.GetSize() / sprite.pixelsPerUnit * transform.lossyScale;
        var size1 = emitterB.box.GetSize() / sprite.pixelsPerUnit * transform.lossyScale;

        //旧的颜色
        var color = Gizmos.color;

        //绘制两个发射器的调试信息
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center0, size0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center1, size1);

        //重置颜色
        Gizmos.color = color;
    }
#endif

    //粒子
    [Serializable]
    public struct SparklyInstance
    {
        public bool alive;    //粒子是否存活
        public float timestart;//粒子开始时间
        public float timeend;  //粒子结束时间
        public Vector2 position;//粒子当前位置
        public Vector2 velocity;//粒子的速度
        public float scale;    //粒子当前的缩放
        public float scale0;   //第0帧的缩放
        public float scale1;   //第1帧的缩放
        public float scale2;   //第2帧的缩放
        public float scale3;   //第3帧的缩放
        public float scale4;   //第4帧的缩放
    };
    //发射器
    [Serializable]
    public unsafe struct SparklyEmitter : IDisposable
    {
        //脚本所在的GameObject的矩阵
        [HideInInspector]
        public Matrix4x4 localToWorld;

        //初始化发射器
        public void Initialize(int capacity)
        {
            spawnRateValue = spawnRate.Random();
            //初始化所有数组
            particles = new NativeArray<SparklyInstance>(capacity, Allocator.Persistent);
            deadParticles = new NativeList<int>(capacity, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(capacity, Allocator.Persistent);

            //一开始没有粒子
            for (int i = capacity - 1; i >= 0; i--)
            {
                deadParticles.Add(i);
            }
        }
        //用于更新数据, 在FixedUpdate中执行, 也可以在Update中运行
        public void UpdateEmitter(float deltaTime = -1f)
        {
            if (deltaTime < 0f)
            {
                deltaTime = Time.fixedDeltaTime;
            }
            //生成粒子
            spawnAccumulate += spawnRateValue * deltaTime;
            var spawnCount = (int)spawnAccumulate;
            spawnAccumulate -= spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                Spawn();
            }

            //更新粒子
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
        //更新数据的作业
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
                //如果粒子已经死亡则跳过
                if (!particle.alive) return;
                //如果时间到了就杀死粒子
                if (time > particle.timeend)
                {
                    //如果粒子已经死亡
                    if (!particles[index].alive)
                    {
                        return;
                    }
                    //杀死粒子
                    particle.alive = false;
                    particles[index] = particle;
                    deadParticles.Add(index);
                    matrices[index] = Matrix4x4.zero; //让粒子消失
                    return;
                }
                //计算渲染使用的矩阵
                particle.position += particle.velocity * deltaTime;
                //计算缩放
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

        //生成粒子
        public int Spawn()
        {
            //如果无法分配新的粒子
            if (deadParticles.Length < 1)
            {
                return -1;
            }
            //分配新的粒子
            var index = deadParticles[^1];
            deadParticles.RemoveAt(deadParticles.Length - 1);
            var particle = particles[index];
            particle.alive = true;
            //设置初始属性
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
            //返回粒子
            return index;
        }
        //杀死粒子
        public void Kill(int index)
        {
            //如果粒子已经死亡
            if (!particles[index].alive)
            {
                return;
            }
            //杀死粒子
            var particle = particles[index];
            particle.alive = false;
            particles[index] = particle;
            deadParticles.Add(index);
            matrices[index] = Matrix4x4.zero; //让粒子消失
        }

        //获取粒子缩放
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

        //释放非托管资源
        public void Dispose()
        {
            particles.Dispose();
            deadParticles.Dispose();
        }

        //所有粒子的数据
        public NativeArray<SparklyInstance> particles;
        //粒子的矩阵
        public NativeArray<Matrix4x4> matrices;

        //死亡粒子的索引
        public NativeList<int> deadParticles;
        public float pixelsToUnit;

        //粒子系统的属性
        public float spawnRateValue;
        public float spawnAccumulate;
        public Range1 spawnRate;
        public Range2 box;
        public Range2 skew;

        //粒子的属性
        public Range1 duration;
        public Range2 velocity;
        public float alpha;

        //缩放的5个关键帧
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
    
    //表示矩形范围
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

        public Range1 x; //x范围
        public Range1 y; //y范围
    }
    //表示浮点数的范围
    [Serializable]
    public struct Range1
    {
        public Range1(float start, float end, Curve distribution = Curve.Linear)
        {
            this.start = start;
            this.end = end;
            this.distribution = distribution;
        }

        //在规定范围中使用曲线随机一个值
        public float Random() => Evaluate(start, end, random.NextFloat(), distribution);
        //获取范围的大小
        public float GetSize() => math.abs(end - start);
        //获取范围的中心
        public float GetCenter() => (start + end) * 0.5f;
        public float start; //起始值
        public float end; //结束值
        public Curve distribution; //曲线
    }

    //使用曲线插值
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
        //两个二次函数

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


        //在前半部分使用第一种
        if (time <= 0.5f)
        {
            return In(time * 2.0f) * 0.5f;
        }
        //在后半部分使用第二种
        return Out((time - 0.5f) * 2.0f) * 0.5f + 0.5f;
    }
    //插值使用的曲线
    [Serializable]
    public enum Curve
    {
        Linear,
        FastInOutWeak
    }
}