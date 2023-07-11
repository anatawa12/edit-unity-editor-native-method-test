# Proof of concept: Remove Window after uploading avatars without modifying VRChat SDK

I want to detect end of uploading avatars & remove dialog after uploading avatars.

However, According to MATERIAL LICENSE AGREEMENT, modifying VRChat SDK is not allowed.

This shows we can remove window after uploading avatars so I created tool to upload multiple avatar continuously with this technic: [ContinuousAvatarUploader](https://github.com/anatawa12/ContinuousAvatarUploader)

## How does this works?

Instead of modifying VRChat SDK, this modifies (replaces) `UnityEditor.EditorUtility.DisplayDialog` function with [Harmony].

However, there's one problem: we cannot just patch native (extern) methods. (see [this issue comment][issue-comment-replace-only])
We need original native method to show dialog but if we patched that method, the original way to call native method is gone.

But, there's a trick. `UnityEditor.EditorUtility.DisplayDialog` is extern method with InternalCall.
extern methods with InternalCall is distinguished using Fully Qualified Name. 
So, if we define method with exactly same fqname, we can call native method. 
That's `UnityEditor.EditorUtility` class in Test.cs.

This PoC shows combing those is work as expected.

[Harmony]: https://harmony.pardeike.net/index.html
[issue-comment-replace-only]: https://github.com/pardeike/Harmony/issues/459#issuecomment-1065935420

## License

MIT License
