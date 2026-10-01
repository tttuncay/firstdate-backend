namespace DateSpot.Core.Enums;

public enum PriceLevel
{
    Budget = 1,      // ₺ (Öğrenci / Uygun Bütçeli)
    Moderate = 2,    // ₺₺ (Orta / Standart Şık)
    Premium = 3      // ₺₺₺ (Lüks / Fine Dining & Özel Kokteyl)
}

public enum NoiseLevel
{
    WhisperQuiet = 1,   // Fısıltı sessizliğinde (Sohbet odaklı)
    ModerateMusic = 2,  // Dengeli arka plan caz/akustik (İdeal)
    LivelyLoud = 3      // Canlı, enerjik ve biraz gürültülü (Buzkıran ortamı)
}

public enum DateConcept
{
    QuietAndIntimate = 1,   // Sessiz & Samimi (Derin Sohbet)
    RomanticAndChic = 2,    // Romantik & Mum Işığı
    CocktailAndVibe = 3,    // Kokteyl Bar & Sosyal
    CoffeeAndWalk = 4,      // Kahve & Tatlı / Sahil Yürüyüşü
    FunAndCasual = 5        // Eğlenceli & Rahat (Oyunlu/Aktiviteli)
}
