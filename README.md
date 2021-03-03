# Subset search

Subset search algorithm that skips subsets, using stepping with size of the needle. If an item presents in the needle, checks if (index of item in the stack) - (index of item in the needle) = (index of the first item of the needle), then starts straight search in the stack while all the items are in the needle. 

The larger the needle, the more efficient the algorithm.

![](gitm/demo.GIF)

