using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Plays imported clips (idle/walk/shoot/hit) via Playables — no AnimatorController required.</summary>
    public sealed class Proto_Greybox3DAnimDriver : MonoBehaviour
    {
        enum Locomo { Idle, Walk }
        enum OneShot { None, Shoot, Hit, Down }

        Animator _animator;
        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        AnimationClipPlayable _idle;
        AnimationClipPlayable _walk;
        AnimationClipPlayable _shoot;
        AnimationClipPlayable _hit;
        bool _ready;
        Locomo _locomo;
        OneShot _shot;
        float _shotEnd;
        float _walkWeight;

        public void Bind(Animator animator, Proto_Greybox3DCatalog cat)
        {
            _animator = animator;
            if (_animator == null || cat == null || cat.CowboyIdle == null) return;

            if (_graph.IsValid()) _graph.Destroy();
            _graph = PlayableGraph.Create("Greybox3D_Char");
            _graph.SetTimeUpdateMode(Application.isPlaying
                ? DirectorUpdateMode.GameTime
                : DirectorUpdateMode.Manual);

            _mixer = AnimationMixerPlayable.Create(_graph, 4);
            _idle = AnimationClipPlayable.Create(_graph, cat.CowboyIdle);
            _walk = AnimationClipPlayable.Create(_graph, cat.CowboyWalk != null ? cat.CowboyWalk : cat.CowboyIdle);
            _shoot = AnimationClipPlayable.Create(_graph, cat.CowboyShoot != null ? cat.CowboyShoot : cat.CowboyIdle);
            _hit = AnimationClipPlayable.Create(_graph, cat.CowboyHit != null ? cat.CowboyHit : cat.CowboyIdle);
            _idle.SetApplyFootIK(false);
            _walk.SetApplyFootIK(false);
            _graph.Connect(_idle, 0, _mixer, 0);
            _graph.Connect(_walk, 0, _mixer, 1);
            _graph.Connect(_shoot, 0, _mixer, 2);
            _graph.Connect(_hit, 0, _mixer, 3);
            SetWeights(1f, 0f, 0f, 0f);

            var output = AnimationPlayableOutput.Create(_graph, "anim", _animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
            _ready = true;
        }

        public void Tick(float time, float dt, bool moving, bool firing, bool hit, bool down, bool rolling)
        {
            if (!_ready) return;
            if (down)
            {
                if (_shot != OneShot.Down)
                {
                    _shot = OneShot.Down;
                    _hit.SetTime(Mathf.Max(0f, _hit.GetAnimationClip().length * 0.85f));
                    _hit.SetSpeed(0f);
                }
                SetWeights(0f, 0f, 0f, 1f);
                if (!Application.isPlaying) _graph.Evaluate(dt);
                return;
            }

            if (hit)
            {
                _shot = OneShot.Hit;
                _shotEnd = time + Mathf.Max(0.25f, _hit.GetAnimationClip().length * 0.7f);
                _hit.SetTime(0);
                _hit.SetSpeed(1.15f);
            }
            else if (firing && _shot == OneShot.None)
            {
                _shot = OneShot.Shoot;
                _shotEnd = time + Mathf.Max(0.18f, _shoot.GetAnimationClip().length * 0.55f);
                _shoot.SetTime(0);
                _shoot.SetSpeed(1.4f);
            }

            if (_shot != OneShot.None && _shot != OneShot.Down && time >= _shotEnd)
                _shot = OneShot.None;

            if (_shot == OneShot.Hit)
            {
                SetWeights(0f, 0f, 0f, 1f);
                if (!Application.isPlaying && _graph.IsValid()) _graph.Evaluate(dt);
                return;
            }

            if (_shot == OneShot.Shoot)
            {
                SetWeights(0.15f, 0f, 0.85f, 0f);
                if (!Application.isPlaying && _graph.IsValid()) _graph.Evaluate(dt);
                return;
            }

            if (rolling)
            {
                SetWeights(1f, 0f, 0f, 0f);
                if (!Application.isPlaying && _graph.IsValid()) _graph.Evaluate(dt);
                return;
            }

            _locomo = moving ? Locomo.Walk : Locomo.Idle;
            float target = moving ? 1f : 0f;
            _walkWeight = Mathf.MoveTowards(_walkWeight, target, dt * 8f);
            _walk.SetSpeed(moving ? 1.35f : 1f);
            SetWeights(1f - _walkWeight, _walkWeight, 0f, 0f);
            if (!Application.isPlaying && _graph.IsValid())
                _graph.Evaluate(dt);
        }

        void SetWeights(float idle, float walk, float shoot, float hit)
        {
            _mixer.SetInputWeight(0, idle);
            _mixer.SetInputWeight(1, walk);
            _mixer.SetInputWeight(2, shoot);
            _mixer.SetInputWeight(3, hit);
        }

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
