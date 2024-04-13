# codeTalks.Backend

Bu uygulama, Patika.dev React Native patikası içerisinde yer alan ödevlerden biri olan codeTalks adlı projenin back-end kısmıdır. Ödevden ayrıca olarak firebase gibi bir servis kullanmak yerine back-end servisini kendim yazdığım ve içerisine signalR eklediğim bir API ile haberleşmesi sağlanmıştır. 
<br />
Ödevin linki : 
https://academy.patika.dev/courses/react-native/odev_5

- <b>Microsoft.AspNetCore.SignalR</b> paketi sayesinde API da yer alan Hub servislerine bağlantı kurup bu servisleri dinleyebiliriz.
- Proje, <b>Clean Architecture</b> mimarisine göre oluşturulmuştur kullanılmaktadır ve MediatR paketi sayesinde <b>observer</b> deseni daha kolay projeye uygulanmıştır.
- Projede ilişkisel veritabanı yönetim sistemi olarak <b>SQL Server </b>kullanılmaktadır
- Proje; Core ve main packages olarak ayrılmış olup genel olarak kullanılan çoğu sınıflar <b> (Repository Pattern, Security, Pagination, Cross Cutting Concerns gibi) </b>, core packages kısmında toplanmıştır.

## TODO

### Yapılacaklar

- [ ] Kullanıcıların profil fotoğraflarının kaydı için Cloudinary servisini entegre et.
- [ ] Kullanıcıların Google ve Facebook ile bağlantı yapmaları için altyapı hazırla.
- [ ] Kullanıcı bilgilerinin güncellenebileceği bir Controller oluştur.
- [ ] Projenin rollendirme kısımlarını Features da yer alan Commands ve Queries sınıflarında tanımla.
    - ISecuredRequest sınıfı sayesinde 
- [ ] Projeyi Dockerize et.
- [ ] Projeyi , Heroku üzerinde deploy etmeyi dene.
    - Sadece bir fikir ???

### Yapım Aşamasında

### Bitti ✓