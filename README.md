# codeTalks.Backend

**codeTalks**, geliştiriciler için tasarlanmış gerçek zamanlı bir sohbet uygulamasının back-end kısmıdır. Firebase gibi hazır bir servis kullanmak yerine back-end servisi özel olarak yazılmış ve içerisine SignalR eklenmiş bir API olarak geliştirilmiştir.

* **Microsoft.AspNetCore.SignalR** paketi sayesinde API'da yer alan Hub servislerine bağlantı kurup bu servisleri dinleyebiliriz.
* Proje, **Clean Architecture** mimarisine göre oluşturulmuştur. MediatR ücretli bir paket haline geldiğinden kaldırılmış, yerine kendi **CQRS handler'larımız** yazılarak **observer** deseni projeye uygulanmıştır.
* Projede ilişkisel veritabanı yönetim sistemi olarak **SQL Server** kullanılmaktadır.
* Proje; Core ve main packages olarak ayrılmış olup genel olarak kullanılan çoğu sınıflar **(Repository Pattern, Security, Pagination, Cross Cutting Concerns gibi)**, core packages kısmında toplanmıştır.

## Roadmap

Proje ilerleyişini [GitHub Projects](https://github.com/users/sahinmaral/projects/5) üzerinden takip edebilirsiniz.

## İlgili Repo

Mobil uygulama kaynak koduna ulaşmak için: [codeTalks Mobile](https://github.com/sahinmaral/codeTalks)
