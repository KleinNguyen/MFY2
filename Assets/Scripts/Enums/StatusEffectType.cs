using System.Collections.Generic;
using UnityEngine;

public enum StatusEffectType 
{
    ARMOR,
    BURN,
    WEAK, // giảm sát thương mà quái gây ra
    POISON, // độc là gây sát thương theo lượt, có thể stack (ví dụ stack 2 thì gây 2 damage mỗi lượt, sau đó trừ đi còn 1)
    EXPOSED, // quái nhận thêm 50% sát thương từ đòn đánh
    ARMOR_BREAK, // quái sẽ giảm % nhận giáp đi
    STRENGTH, // tăng sát thương gây ra
    DEXTERITY // tăng giáp được nhận
}
