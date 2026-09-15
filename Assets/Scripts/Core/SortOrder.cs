using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Thu tu ve cua tran dau, chia DAI so trong mot sorting layer (Default), duoi len tren:
    /// nen -> lau dai -> vet tren dat (o hero, vung bang, vong an ky) -> UNIT (sort theo chan) -> dan/vet chem
    /// -> hang cay truoc -> thanh mau.
    ///
    /// Unit tu dat order MOI FRAME tu y cua chan, khong dua vao `transparencySortAxis`: hai con dung cung
    /// mot hang pixel thi truc sort cho hoa nhau va Unity ve luc con nay luc con kia (nhap nhay khi dang vay
    /// danh). Hang pixel nhan 8, tru 0/2/4/6 theo instance id -> luon co thu tu co dinh, va con trong
    /// khoang le cho FX gan tren than (vo da, dau an ky) ve ngay tren chu cua no.
    /// </summary>
    public static class SortOrder
    {
        public const int Background = -100;
        public const int Castle = 0;
        public const int GroundDecal = 5;

        // Man cao 10,8 unit -> hang pixel [-540; 540] x 8 -> order unit [680; 9320].
        const int UnitBase = 5000;

        public const int Air = 9500;
        public const int Foreground = 9600;
        public const int Overlay = 9700;

        public static int ForUnit(Component unit) =>
            UnitBase - Mathf.RoundToInt(unit.transform.position.y * 100f) * 8 - (unit.GetInstanceID() & 3) * 2;
    }
}
