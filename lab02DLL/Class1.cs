namespace lab02DLL;


public static class Functions {
    public static int CountCharacter(string sentence, char symbol) {
        var count = 0;

        foreach (var c in sentence) {
            if (c == symbol) {
                count++;
            }
        }

        return count;
    }


    // Варіант 1:
    // створює масив, заповнює його та повертає
    public static int[] FillRandomArray(int size) {
        int[] array = new int[size];

        FillRandomArray(array);

        return array;
    }


    // Варіант 2:
    // заповнює вже наявний масив
    private static void FillRandomArray(int[] array) {
        for (var i = 0; i < array.Length; i++) {
            array[i] = Random.Shared.Next(-150, 161);
        }
    }
}